using System;

namespace GravityDefied
{
    // Port of class 'l': a single level's terrain polyline plus rendering.
    // Coordinates loaded as raw<<13; (value<<3>>16) converts back to pixels.
    internal sealed class Level
    {
        private int a = 0;   // view min x
        private int d = 0;   // view max x
        private int e = 0;   // perspective ref x (start)
        private int b = 0;   // perspective ref x (finish)
        private int g = 0;   // perspective ref y
        public int Cr0;      // header[0] (start bound)
        public int Cr1;      // header[1]
        public int Cr2;      // finish bound
        public int StartIdx = 0;   // cfr_renamed_3
        public int FinishIdx = 0;  // cfr_renamed_4
        public int Cr5;
        public int NumPts;   // cfr_renamed_6
        public int Cr7;
        public int[][] Pts = null;   // cfr_renamed_8
        public string Name = "levelname";
        private int r = 0;   // shading accumulator

        public Level()
        {
            Reset();
        }

        // l.cfr_renamed_7()
        public void Reset()
        {
            Cr0 = 0;
            Cr1 = 0;
            Cr2 = 0xC80000;
            NumPts = 0;
            Cr7 = 0;
        }

        public int StartXRaw() => Cr0 << 3 >> 16;
        public int Cr1Acc() => Cr1 << 3 >> 16;
        public int Cr2Acc() => Cr2 << 3 >> 16;
        public int Cr5Acc() => Cr5 << 3 >> 16;
        public int NodeX(int n) => Pts[n][0] << 3 >> 16;
        public int NodeY(int n) => Pts[n][1] << 3 >> 16;

        // l.cfr_renamed_11(int) : progress fraction (0..65536) between start and finish nodes
        public int Progress(int n2)
        {
            int n3 = n2 - Pts[StartIdx][0];
            int n4 = Pts[FinishIdx][0] - Pts[StartIdx][0];
            if ((n4 < 0 ? -n4 : n4) < 3 || n3 > n4) return 65536;
            return (int)(((long)n3 << 32) / (long)n4 >> 16);
        }

        // l.cfr_renamed_10(int,int) : set view window x bounds
        public void SetView(int n2, int n3)
        {
            a = n2 << 16 >> 3;
            d = n3 << 16 >> 3;
        }

        // l.a(int,int) : set perspective start/finish refs
        public void CamSet(int n2, int n3)
        {
            e = n2 >> 1;
            b = n3 >> 1;
        }

        // l.a(int,int,int) : set perspective refs + ground y
        public void SetPersp3(int n2, int n3, int n4)
        {
            e = n2;
            b = n3;
            g = n4;
        }

        // l.cfr_renamed_10(i,int,int) : perspective ground shading
        public void DrawShading(Renderer i2, int n2, int n3)
        {
            if (n3 <= NumPts - 1)
            {
                int n5 = g - (Pts[n2][1] + Pts[n3 + 1][1] >> 1) < 0 ? 0 : g - (Pts[n2][1] + Pts[n3 + 1][1] >> 1);
                int n4 = n5;
                if (g <= Pts[n2][1] || g <= Pts[n3 + 1][1])
                    n4 = n4 < 327680 ? n4 : 327680;
                r = (int)((long)r * 49152L >> 16) + (int)((long)n4 * 16384L >> 16);
                if (r <= 557056)
                {
                    int n7 = (int)(0x190000L * (long)r >> 16) >> 16;
                    int n6 = n7;
                    i2.SetColor(n7, n7, n6);
                    int n8 = Pts[n2][0] - Pts[n2 + 1][0];
                    int n9 = (int)(((long)(Pts[n2][1] - Pts[n2 + 1][1]) << 32) / (long)n8 >> 16);
                    int n10 = Pts[n2][1] - (int)((long)Pts[n2][0] * (long)n9 >> 16);
                    int n11 = (int)((long)e * (long)n9 >> 16) + n10;
                    n8 = Pts[n3][0] - Pts[n3 + 1][0];
                    n9 = (int)(((long)(Pts[n3][1] - Pts[n3 + 1][1]) << 32) / (long)n8 >> 16);
                    n10 = Pts[n3][1] - (int)((long)Pts[n3][0] * (long)n9 >> 16);
                    int n12 = (int)((long)b * (long)n9 >> 16) + n10;
                    if (n2 == n3)
                    {
                        i2.Line(e << 3 >> 16, n11 + 65536 << 3 >> 16, b << 3 >> 16, n12 + 65536 << 3 >> 16);
                        return;
                    }
                    i2.Line(e << 3 >> 16, n11 + 65536 << 3 >> 16, Pts[n2 + 1][0] << 3 >> 16, Pts[n2 + 1][1] + 65536 << 3 >> 16);
                    for (int i3 = n2 + 1; i3 < n3; ++i3)
                        i2.Line(Pts[i3][0] << 3 >> 16, Pts[i3][1] + 65536 << 3 >> 16, Pts[i3 + 1][0] << 3 >> 16, Pts[i3 + 1][1] + 65536 << 3 >> 16);
                    i2.Line(Pts[n3][0] << 3 >> 16, Pts[n3][1] + 65536 << 3 >> 16, b << 3 >> 16, n12 + 65536 << 3 >> 16);
                }
            }
        }

        // l.a(i,int,int) : perspective terrain
        public void DrawPersp(Renderer i2, int n2, int n3)
        {
            int n7 = 0;
            int n8 = 0;
            int n6;
            for (n6 = 0; n6 < NumPts - 1 && Pts[n6][0] <= a; ++n6) { }
            if (n6 > 0) --n6;
            int n9 = n2 - Pts[n6][0];
            int n10 = n3 + 0x320000 - Pts[n6][1];
            int n11 = Bike.Len(n9, n10);
            n9 = (int)(((long)n9 << 32) / (long)(n11 >> 1 >> 1) >> 16);
            n10 = (int)(((long)n10 << 32) / (long)(n11 >> 1 >> 1) >> 16);
            i2.SetColor(0, 170, 0);
            int n5, n4;
            while (n6 < NumPts - 1)
            {
                n5 = n9;
                n4 = n10;
                n9 = n2 - Pts[n6 + 1][0];
                n10 = n3 + 0x320000 - Pts[n6 + 1][1];
                n11 = Bike.Len(n9, n10);
                n9 = (int)(((long)n9 << 32) / (long)(n11 >> 1 >> 1) >> 16);
                n10 = (int)(((long)n10 << 32) / (long)(n11 >> 1 >> 1) >> 16);
                i2.Line(Pts[n6][0] + n5 << 3 >> 16, Pts[n6][1] + n4 << 3 >> 16, Pts[n6 + 1][0] + n9 << 3 >> 16, Pts[n6 + 1][1] + n10 << 3 >> 16);
                i2.Line(Pts[n6][0] << 3 >> 16, Pts[n6][1] << 3 >> 16, Pts[n6][0] + n5 << 3 >> 16, Pts[n6][1] + n4 << 3 >> 16);
                if (n6 > 1)
                {
                    if (Pts[n6][0] > e && n7 == 0) n7 = n6 - 1;
                    if (Pts[n6][0] > b && n8 == 0) n8 = n6 - 1;
                }
                if (StartIdx == n6)
                {
                    i2.StartFlag(Pts[StartIdx][0] + n5 << 3 >> 16, Pts[StartIdx][1] + n4 << 3 >> 16);
                    i2.SetColor(0, 170, 0);
                }
                if (FinishIdx == n6)
                {
                    i2.FinishFlag(Pts[FinishIdx][0] + n5 << 3 >> 16, Pts[FinishIdx][1] + n4 << 3 >> 16);
                    i2.SetColor(0, 170, 0);
                }
                if (Pts[n6][0] > d) break;
                ++n6;
            }
            n5 = n9;
            n4 = n10;
            i2.Line(Pts[NumPts - 1][0] << 3 >> 16, Pts[NumPts - 1][1] << 3 >> 16, Pts[NumPts - 1][0] + n5 << 3 >> 16, Pts[NumPts - 1][1] + n4 << 3 >> 16);
            if (Levels.Shading) DrawShading(i2, n7, n8);
        }

        // l.a(i) : flat terrain
        public void DrawFlat(Renderer i2)
        {
            int n2;
            for (n2 = 0; n2 < NumPts - 1 && Pts[n2][0] <= a; ++n2) { }
            if (n2 > 0) --n2;
            while (n2 < NumPts - 1)
            {
                i2.Line(Pts[n2][0] << 3 >> 16, Pts[n2][1] << 3 >> 16, Pts[n2 + 1][0] << 3 >> 16, Pts[n2 + 1][1] << 3 >> 16);
                if (StartIdx == n2)
                {
                    i2.StartFlag(Pts[StartIdx][0] << 3 >> 16, Pts[StartIdx][1] << 3 >> 16);
                    i2.SetColor(0, 255, 0);
                }
                if (FinishIdx == n2)
                {
                    i2.FinishFlag(Pts[FinishIdx][0] << 3 >> 16, Pts[FinishIdx][1] << 3 >> 16);
                    i2.SetColor(0, 255, 0);
                }
                if (Pts[n2][0] > d) break;
                ++n2;
            }
        }

        // l.cfr_renamed_4(int,int)
        public void AddPointRaw(int n2, int n3)
        {
            AddPoint(n2 << 16 >> 3, n3 << 16 >> 3);
        }

        // l.cfr_renamed_11(int,int)
        public void AddPoint(int n2, int n3)
        {
            if (Pts == null || Pts.Length <= NumPts)
            {
                int n4 = 100;
                if (Pts != null) n4 = n4 < Pts.Length + 30 ? Pts.Length + 30 : n4;
                int[][] arr = new int[n4][];
                for (int i = 0; i < n4; ++i) arr[i] = new int[2];
                if (Pts != null) Array.Copy(Pts, 0, arr, 0, Pts.Length);
                Pts = arr;
            }
            if (NumPts == 0 || Pts[NumPts - 1][0] < n2)
            {
                Pts[NumPts][0] = n2;
                Pts[NumPts][1] = n3;
                ++NumPts;
            }
        }

        // l.a(DataInputStream) : load polyline from the byte stream
        public void Load(BeReader r2)
        {
            Reset();
            if (r2.ReadByte() == 50)
            {
                r2.Skip(20);
            }
            FinishIdx = 0;
            StartIdx = 0;
            Cr0 = r2.ReadInt();
            Cr1 = r2.ReadInt();
            Cr2 = r2.ReadInt();
            Cr5 = r2.ReadInt();
            int n2 = r2.ReadShort();
            int n3 = r2.ReadInt();
            int n4 = r2.ReadInt();
            int n5 = n3;
            int n6 = n4;
            AddPointRaw(n5, n6);
            for (int i = 1; i < n2; ++i)
            {
                sbyte by = r2.ReadByte();
                if (by == -1)
                {
                    n6 = 0;
                    n5 = 0;
                    n3 = r2.ReadInt();
                    n4 = r2.ReadInt();
                }
                else
                {
                    n3 = by;
                    n4 = r2.ReadByte();
                }
                AddPointRaw(n5 += n3, n6 += n4);
            }
        }
    }
}
