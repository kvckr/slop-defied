using System;
using System.IO;

namespace GravityDefied
{
    // Port of class 'f': level index/loading from levels.mrg, view-window
    // management, and terrain collision detection.
    internal sealed class Levels
    {
        public static bool Perspective;   // cfr_renamed_6
        public static bool Shading;        // cfr_renamed_1

        private int[][] s = null;          // segment normals
        public Level g = null;             // current level
        private int[] h = new int[3];      // outer radius² per body type
        private int[] v = new int[3];      // inner radius²
        public int CurGroup = 0;           // cfr_renamed_0
        public int CurLevel = -1;          // f
        public string[][] Names = new string[3][];
        private int[][] c = new int[3][];  // file offsets
        public int j, i;                   // dims
        public int MaxX;                   // cfr_renamed_2
        private int da = 0;                // capacity
        private int ea = 0, fa = 0;        // view window node indices
        private int a = 0, k = 0;          // view window x positions
        public int e, d;                   // collision normal output

        private byte[] mrg;

        public Levels()
        {
            mrg = LoadResource("levels.mrg");
            for (int n = 0; n < 3; ++n)
            {
                h[n] = (int)((long)(Bike.Radii[n] + 19660 >> 1) * (long)(Bike.Radii[n] + 19660 >> 1) >> 16);
                v[n] = (int)((long)(Bike.Radii[n] - 19660 >> 1) * (long)(Bike.Radii[n] - 19660 >> 1) >> 16);
            }
            LoadIndex();
            LoadCurrent();
        }

        private static byte[] LoadResource(string name)
        {
            string baseDir = AppContext.BaseDirectory;
            string path = Path.Combine(baseDir, "Assets", name);
            if (!File.Exists(path)) path = Path.Combine(baseDir, name);
            return File.ReadAllBytes(path);
        }

        // f.d() : read the level index (counts, offsets, names)
        private void LoadIndex()
        {
            BeReader r = new BeReader(mrg, 0);
            for (int grp = 0; grp < 3; ++grp)
            {
                int count = r.ReadInt();
                c[grp] = new int[count];
                Names[grp] = new string[count];
                for (int lvl = 0; lvl < count; ++lvl)
                {
                    c[grp][lvl] = r.ReadInt();
                    byte[] buf = new byte[40];
                    for (int b = 0; b < 40; ++b)
                    {
                        buf[b] = (byte)r.ReadUByte();
                        if (buf[b] == 0)
                        {
                            Names[grp][lvl] = System.Text.Encoding.Latin1.GetString(buf, 0, b).Replace('_', ' ');
                            break;
                        }
                    }
                }
            }
        }

        // f.a(int,int) : level name
        public string Name(int n2, int n3)
        {
            if (n2 < Names.Length && n3 < Names[n2].Length) return Names[n2][n3];
            return "---";
        }

        // f.cfr_renamed_10()
        public void LoadCurrent()
        {
            SelectLevel(CurGroup, CurLevel + 1);
        }

        // f.cfr_renamed_11(int,int)
        public int SelectLevel(int n2, int n3)
        {
            CurGroup = n2;
            CurLevel = n3;
            if (CurLevel >= Names[CurGroup].Length) CurLevel = 0;
            LoadBody(CurGroup + 1, CurLevel + 1);
            return CurLevel;
        }

        // f.h(int,int) : seek to offset and load the level body
        private void LoadBody(int n2, int n3)
        {
            int offset = c[n2 - 1][n3 - 1];
            BeReader r = new BeReader(mrg, offset);
            if (g == null) g = new Level();
            g.Load(r);
            Preprocess(g);
        }

        // f.cfr_renamed_10(int) : dimensions
        public void SetDims(int n2)
        {
            j = g.Cr0 << 1;
            i = g.Cr1 << 1;
        }

        public int FinishX() => g.Pts[g.FinishIdx][0] << 1;       // f.cfr_renamed_11()
        public int StartNodeX() => g.Pts[g.StartIdx][0] << 1;     // f.cfr_renamed_7()
        public int LevelStartX() => g.Cr0 << 1;                   // f.cfr_renamed_13()
        public int LevelStartY() => g.Cr1 << 1;                   // f.a()
        public int TerrainIndexAtX(int n2) => g.Progress(n2 >> 1);// f.a(int)

        // f.a(l) : precompute segment normals and start/finish node indices
        public void Preprocess(Level lv)
        {
            MaxX = int.MinValue;
            g = lv;
            int n2 = g.NumPts;
            if (s == null || da < n2)
            {
                s = null;
                da = n2 < 100 ? 100 : n2;
                s = new int[da][];
                for (int t = 0; t < da; ++t) s[t] = new int[2];
            }
            ea = 0; fa = 0;
            a = lv.Pts[ea][0];
            k = lv.Pts[fa][0];
            for (int idx = 0; idx < n2; ++idx)
            {
                int dx = lv.Pts[(idx + 1) % n2][0] - lv.Pts[idx][0];
                int dy = lv.Pts[(idx + 1) % n2][1] - lv.Pts[idx][1];
                if (idx != 0 && idx != n2 - 1)
                    MaxX = MaxX < lv.Pts[idx][0] ? lv.Pts[idx][0] : MaxX;
                int nx = -dy;
                int ny = dx;
                int len = Bike.Len(nx, ny);
                s[idx][0] = (int)(((long)nx << 32) / (long)len >> 16);
                s[idx][1] = (int)(((long)ny << 32) / (long)len >> 16);
                if (g.StartIdx == 0 && lv.Pts[idx][0] > g.Cr0) g.StartIdx = idx + 1;
                if (g.FinishIdx != 0 || lv.Pts[idx][0] <= g.Cr2) continue;
                g.FinishIdx = idx;
            }
            ea = 0; fa = 0; a = 0; k = 0;
        }

        // f.cfr_renamed_10(int,int)
        public void CamWindow(int n2, int n3) => g.SetView(n2, n3);

        // f.a(i,int,int) : perspective terrain
        public void DrawTerrainPersp(Renderer i2, int n2, int n3)
        {
            i2.SetColor(0, 170, 0);
            g.DrawPersp(i2, n2 >> 1, n3 >> 1);
        }

        // f.a(i) : flat terrain
        public void DrawTerrain(Renderer i2)
        {
            i2.SetColor(0, 255, 0);
            g.DrawFlat(i2);
        }

        // f.a(int,int,int) : update view window
        public void LevelView(int n2, int n3, int n4)
        {
            g.SetPersp3(n2 + 98304 >> 1, n3 - 98304 >> 1, n4 >> 1);
            n2 >>= 1;
            fa = fa < g.NumPts - 1 ? fa : g.NumPts - 1;
            ea = ea < 0 ? 0 : ea;
            if ((n3 >>= 1) > k)
            {
                while (fa < g.NumPts - 1 && n3 > g.Pts[++fa][0]) { }
            }
            else if (n2 < a)
            {
                while (ea > 0 && n2 < g.Pts[--ea][0]) { }
            }
            else
            {
                while (ea < g.NumPts && n2 > g.Pts[++ea][0]) { }
                if (ea > 0) --ea;
                while (fa > 0 && n3 < g.Pts[--fa][0]) { }
                fa = fa + 1 < g.NumPts - 1 ? fa + 1 : g.NumPts - 1;
            }
            a = g.Pts[ea][0];
            k = g.Pts[fa][0];
        }

        // f.a(n,int) : terrain collision for one node; sets e,d normal; returns 0/1/2
        public int Collide(Node n2, int n3)
        {
            int n4 = 0;
            int n5 = 2;
            int n6 = n2.Px >> 1;
            int n7 = n2.Py >> 1;
            if (Perspective) n7 -= 65536;
            int n8 = 0;
            int n9 = 0;
            for (int idx = ea; idx < fa; ++idx)
            {
                int n12 = g.Pts[idx][0];
                int n13 = g.Pts[idx][1];
                int n14 = g.Pts[idx + 1][0];
                int n15 = g.Pts[idx + 1][1];
                if (n6 - h[n3] > n14 || n6 + h[n3] < n12) continue;
                int n17 = n12 - n14;
                int n18 = n13 - n15;
                int n19 = (int)((long)n17 * (long)n17 >> 16) + (int)((long)n18 * (long)n18 >> 16);
                int n20 = (int)((long)(n6 - n12) * (long)(-n17) >> 16) + (int)((long)(n7 - n13) * (long)(-n18) >> 16);
                int n21 = (n19 < 0 ? -n19 : n19) >= 3
                    ? (int)(((long)n20 << 32) / (long)n19 >> 16)
                    : (n20 > 0 ? 1 : -1) * (n19 > 0 ? 1 : -1) * int.MaxValue;
                if (n21 < 0) n21 = 0;
                if (n21 > 65536) n21 = 65536;
                int n11 = n12 + (int)((long)n21 * (long)(-n17) >> 16);
                int n10 = n13 + (int)((long)n21 * (long)(-n18) >> 16);
                n17 = n6 - n11;
                n18 = n7 - n10;
                long l2 = ((long)n17 * (long)n17 >> 16) + ((long)n18 * (long)n18 >> 16);
                int n22 = l2 < (long)h[n3] ? (l2 >= (long)v[n3] ? 1 : 0) : 2;
                if (n22 == 0 && (int)((long)s[idx][0] * (long)n2.Vx >> 16) + (int)((long)s[idx][1] * (long)n2.Vy >> 16) < 0)
                {
                    e = s[idx][0];
                    d = s[idx][1];
                    return 0;
                }
                if (n22 != 1 || (int)((long)s[idx][0] * (long)n2.Vx >> 16) + (int)((long)s[idx][1] * (long)n2.Vy >> 16) >= 0) continue;
                n5 = 1;
                if (++n4 == 1)
                {
                    n8 = s[idx][0];
                    n9 = s[idx][1];
                    continue;
                }
                n8 += s[idx][0];
                n9 += s[idx][1];
            }
            if (n5 == 1)
            {
                if ((int)((long)n8 * (long)n2.Vx >> 16) + (int)((long)n9 * (long)n2.Vy >> 16) >= 0) return 2;
                e = n8;
                d = n9;
            }
            return n5;
        }

        static Levels()
        {
            Perspective = true;
            Shading = true;
        }
    }
}
