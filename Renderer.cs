using System;
using System.IO;
using Raylib_cs;

namespace GravityDefied
{
    // Port of class 'i' (the Canvas) onto raylib-cs.
    // Holds the camera transform (q/r), the loaded sprite sheets, and all the
    // drawing primitives the physics & level code call. Drawing must occur
    // inside a BeginDrawing/EndDrawing pair (managed by Game).
    internal sealed class Renderer
    {
        private int X;            // camera offset x
        private int B;            // camera offset y
        public int ab;            // width
        public int d;             // game height (above HUD)
        public int l;             // full height
        private int T = 0, Q = 0; // HUD scroll offsets

        private Bike y;

        private Color cur = new Color(0, 0, 0, 255);

        // sprite sheets
        private Texture2D helmet; public int am, cr0;   // helmet frame w/h
        private Texture2D engine; private int engW, engB;
        private Texture2D fender; private int fenW, fenQ;
        private Texture2D[] limb = new Texture2D[3];     // 0=arm,1=leg,2=body
        private int[] limbW = new int[3];
        private int[] limbH = new int[3];
        private Texture2D sprites;                        // UI icon sheet
        private bool spritesLoaded;
        private Texture2D raster, logo, splash;           // menu/splash art

        // UI sprite-sheet table (from class i static block)
        public static readonly int[] IcoX = { 0, 0, 15, 15, 15, 0, 6, 12, 18, 18, 25, 25, 25, 37, 37, 37, 15, 32 };
        public static readonly int[] IcoY = { 10, 25, 16, 20, 10, 0, 0, 0, 8, 0, 0, 6, 12, 0, 6, 12, 29, 18 };
        public static readonly int[] IcoW = { 15, 15, 8, 8, 3, 6, 6, 6, 7, 7, 12, 12, 12, 12, 12, 12, 16, 17 };
        public static readonly int[] IcoH = { 15, 15, 4, 4, 3, 10, 10, 10, 8, 8, 6, 6, 6, 6, 6, 6, 11, 22 };

        private readonly int[] jFlag = { 12, 10, 11, 10 };   // start-flag wind frames
        private readonly int[] eFlag = { 14, 13, 15, 13 };   // finish-flag wind frames
        private static int V = 0;    // wind animation phase
        private static int vc = 0;

        // input
        private readonly sbyte[][] D =
        {
            new sbyte[]{0,0}, new sbyte[]{1,0}, new sbyte[]{0,-1}, new sbyte[]{0,0},
            new sbyte[]{0,0}, new sbyte[]{0,1}, new sbyte[]{-1,0}
        };
        private readonly sbyte[][][] m =
        {
            new sbyte[][]{ new sbyte[]{0,0}, new sbyte[]{1,-1}, new sbyte[]{1,0}, new sbyte[]{1,1}, new sbyte[]{0,-1}, new sbyte[]{-1,0}, new sbyte[]{0,1}, new sbyte[]{-1,-1}, new sbyte[]{-1,0}, new sbyte[]{-1,1} },
            new sbyte[][]{ new sbyte[]{0,0}, new sbyte[]{1,0}, new sbyte[]{0,0}, new sbyte[]{0,0}, new sbyte[]{-1,0}, new sbyte[]{0,-1}, new sbyte[]{0,1}, new sbyte[]{0,0}, new sbyte[]{0,0}, new sbyte[]{0,0} },
            new sbyte[][]{ new sbyte[]{0,0}, new sbyte[]{0,0}, new sbyte[]{0,0}, new sbyte[]{1,0}, new sbyte[]{0,-1}, new sbyte[]{0,1}, new sbyte[]{-1,0}, new sbyte[]{0,0}, new sbyte[]{0,0}, new sbyte[]{0,0} }
        };
        private int keyset = 0;    // 'x'

        public int Width => ab;
        public int Height => l;

        public Renderer(int width, int height)
        {
            ab = width;
            l = d = height;
            LoadSprites();
        }

        private static Texture2D Load(string name)
        {
            string baseDir = AppContext.BaseDirectory;
            string path = Path.Combine(baseDir, "Assets", name);
            if (!File.Exists(path)) path = Path.Combine(baseDir, name);
            return Raylib.LoadTexture(path);
        }

        private void LoadSprites()
        {
            helmet = Load("helmet.png");
            am = helmet.Width / 6; cr0 = helmet.Height / 6;
            engine = Load("engine.png");
            engW = engine.Width / 6; engB = engine.Height / 6;
            fender = Load("fender.png");
            fenW = fender.Width / 6; fenQ = fender.Height / 6;
            limb[1] = Load("blueleg.png"); limbW[1] = limb[1].Width / 6; limbH[1] = limb[1].Height / 3;
            limb[0] = Load("bluearm.png"); limbW[0] = limb[0].Width / 6; limbH[0] = limb[0].Height / 3;
            limb[2] = Load("bluebody.png"); limbW[2] = limb[2].Width / 6; limbH[2] = limb[2].Height / 3;
            sprites = Load("sprites.png");
            spritesLoaded = true;
            raster = Load("raster.png");
            logo = Load("logo.png");
            splash = Load("splash.png");
        }

        // m.a(Graphics) : tile the raster pattern over the whole screen
        public void DrawRaster()
        {
            for (int yy = 0; yy < l; yy += raster.Height)
                for (int xx = 0; xx < ab; xx += raster.Width)
                    Raylib.DrawTexture(raster, xx, yy, Color.White);
        }

        public void DrawSplash(int phase)
        {
            Raylib.DrawRectangle(0, 0, ab, l, Color.White);
            Texture2D t = phase == 1 ? logo : splash;
            Raylib.DrawTexture(t, ab / 2 - t.Width / 2, l / 2 - t.Height / 2, Color.White);
        }

        public void DrawMessage(string text)
        {
            int tw = Raylib.MeasureText(text, 16);
            Raylib.DrawText(text, ab / 2 - tw / 2, d / 4, 16, Color.Black);
        }

        public void SetBike(Bike b)
        {
            y = b;
            y.SetCamMax(ab < d ? ab : d);
        }

        // i.cfr_renamed_11(int,int) : set camera offset, update bike view window
        public void SetCamera(int n2, int n3)
        {
            X = n2;
            B = n3;
            y.SetCamWindow(-n2, -n2 + ab);
        }

        private int q(int n) => n + X;
        private int r(int n) => -n + B;

        // ===== primitives =====

        public void Clear()  // i.cfr_renamed_9()
        {
            Raylib.DrawRectangle(0, 0, ab, d, Color.White);
        }

        public void SetColor(int rr, int gg, int bb)  // i.cfr_renamed_10(int,int,int)
        {
            if (Game.InMenu)
            {
                gg += 128; bb += 128; rr += 128;
                if (rr > 240) rr = 240;
                if (gg > 240) gg = 240;
                if (bb > 240) bb = 240;
            }
            cur = new Color(rr, gg, bb, 255);
        }

        public void Line(int x1, int y1, int x2, int y2)  // i.a(int,int,int,int)
        {
            Raylib.DrawLine(q(x1), r(y1), q(x2), r(y2), cur);
        }

        public void LineFx(int x1, int y1, int x2, int y2)  // i.cfr_renamed_11(4 ints)
        {
            Raylib.DrawLine(q(x1 << 2 >> 16), r(y1 << 2 >> 16), q(x2 << 2 >> 16), r(y2 << 2 >> 16), cur);
        }

        public void Circle(int n2, int n3, int n4)  // i.cfr_renamed_11(3 ints)
        {
            int rad = n4 / 2;
            DrawArcOutline(q(n2), r(n3), rad, 0, 360);
        }

        public void WheelArc(int n2, int n3, int n4, int n5)  // i.cfr_renamed_10(4 ints)
        {
            ++n4;
            int deg = -((int)(((long)((int)((long)n5 * 0xB40000L >> 16)) << 32) / 205887L >> 16));
            if (deg < 0) deg += 360;
            DrawArcOutline(q(n2), r(n3), n4, (deg >> 16) + 170, 90);
        }

        public void FillRect(int n2, int n3, int n4, int n5)  // i.cfr_renamed_4(4 ints)
        {
            Raylib.DrawRectangle(q(n2), r(n3), n4, n5, cur);
        }

        // i.a(int,int,int,int,int)
        public void Limb(int n2, int n3, int n4, int n5, int n6) => Limb(n2, n3, n4, n5, n6, 32768);

        // i.a(int,int,int,int,int,int) : driver limb sprite stretched along a segment
        public void Limb(int n2, int n3, int n4, int n5, int n6, int n7)
        {
            int n8 = q((int)((long)n4 * (long)n7 >> 16) + (int)((long)n2 * (long)(65536 - n7) >> 16) >> 16);
            int n9 = r((int)((long)n5 * (long)n7 >> 16) + (int)((long)n3 * (long)(65536 - n7) >> 16) >> 16);
            int n10 = FpMath.Atan2(n4 - n2, n5 - n3);
            int n11 = FrameIndex(n10, 0, 205887, 16, false);
            DrawFrame(limb[n6], limbW[n6], limbH[n6], n11, n8, n9);
        }

        public void Peg(int n2, int n3)  // i.a(int,int) : sprite 4
        {
            int n4 = IcoW[4] / 2;
            int n5 = IcoH[4] / 2;
            DrawIcon(4, q(n2 - n4), r(n3 + n5));
        }

        public void Helmet(int n2, int n3, int n4)  // i.cfr_renamed_13(3 ints)
        {
            int n5 = FrameIndex(n4, -102943, 411774, 32, true);
            DrawFrame(helmet, am, cr0, n5, q(n2), r(n3));
        }

        public void Engine(int n2, int n3, int n4)  // i.cfr_renamed_7(3 ints)
        {
            int n5 = FrameIndex(n4, -247063, 411774, 32, true);
            DrawFrame(engine, engW, engB, n5, q(n2), r(n3));
        }

        public void Wheel(int n2, int n3, int n4)  // i.a(3 ints) : fender sprite
        {
            int n5 = FrameIndex(n4, -185297, 411774, 32, true);
            DrawFrame(fender, fenW, fenQ, n5, q(n2), r(n3));
        }

        public void WheelIcon(int n2, int n3, int n4)  // i.cfr_renamed_4(3 ints)
        {
            int n5 = n4 == 1 ? 0 : 1;
            int n6 = IcoW[n5] / 2;
            int n7 = IcoH[n5] / 2;
            DrawIcon(n5, q(n2 - n6), r(n3 + n7));
        }

        public void StartFlag(int n2, int n3)  // i.cfr_renamed_4(2 ints)
        {
            if (V > 229376) V = 0;
            SetColor(0, 0, 0);
            Line(n2, n3, n2, n3 + 32);
            DrawIcon(jFlag[V >> 16], q(n2), r(n3) - 32);
        }

        public void FinishFlag(int n2, int n3)  // i.cfr_renamed_7(2 ints)
        {
            if (V > 229376) V = 0;
            SetColor(0, 0, 0);
            Line(n2, n3, n2, n3 + 32);
            DrawIcon(eFlag[V >> 16], q(n2), r(n3) - 32);
        }

        // i.a(int,int,int,int,boolean) : map an angle to a frame index
        public int FrameIndex(int n2, int n3, int n4, int n5, bool reverse)
        {
            n2 += n3;
            while (n2 < 0) n2 += n4;
            while (n2 >= n4) n2 -= n4;
            if (reverse) n2 = n4 - n2;
            int n6 = (int)((long)((int)(((long)n2 << 32) / (long)n4 >> 16)) * (long)(n5 << 16) >> 16);
            if (n6 >> 16 < n5 - 1) return n6 >> 16;
            return n5 - 1;
        }

        // i.cfr_renamed_11() : advance wind/flag animation
        public static void WindTick()
        {
            vc += 655;
            int sn = FpMath.Sin(vc);
            int n2 = 32768 + ((sn < 0 ? -sn : sn) >> 1);
            V += (int)(6553L * (long)n2 >> 16);
        }

        // ===== sprite-sheet helpers =====

        private void DrawFrame(Texture2D tex, int fw, int fh, int frame, int cx, int cy)
        {
            int col = frame % 6;
            int row = frame / 6;
            Rectangle src = new Rectangle(col * fw, row * fh, fw, fh);
            Raylib.DrawTextureRec(tex, src, new System.Numerics.Vector2(cx - fw / 2, cy - fh / 2), Color.White);
        }

        // i.a(Graphics,int,int,int) : blit one UI icon at screen (x,y)
        public void DrawIcon(int idx, int x, int y)
        {
            if (!spritesLoaded) return;
            Rectangle src = new Rectangle(IcoX[idx], IcoY[idx], IcoW[idx], IcoH[idx]);
            Raylib.DrawTextureRec(sprites, src, new System.Numerics.Vector2(x, y), Color.White);
        }

        // arc/circle outline emulating java.awt drawArc (CCW from East, y-down screen)
        private void DrawArcOutline(int cx, int cy, int radius, int startDeg, int sweepDeg)
        {
            if (radius <= 0) return;
            int steps = Math.Max(6, Math.Abs(sweepDeg) / 6);
            double prevX = 0, prevY = 0;
            for (int s = 0; s <= steps; ++s)
            {
                double ang = (startDeg + (double)sweepDeg * s / steps) * Math.PI / 180.0;
                double px = cx + radius * Math.Cos(ang);
                double py = cy - radius * Math.Sin(ang);
                if (s > 0)
                    Raylib.DrawLine((int)prevX, (int)prevY, (int)px, (int)py, cur);
                prevX = px; prevY = py;
            }
        }

        // ===== HUD =====

        // i.a(long) : draw the lap timer
        public void DrawTimer(long tenths)
        {
            int n2 = (int)(tenths / 100L);
            int n3 = (int)(tenths % 100L);
            string sec0 = n2 % 60 >= 10 ? "" : "0";
            string main = "" + n2 / 60 + ":" + sec0 + n2 % 60 + ".";
            string cs0 = n3 >= 10 ? "" : "0";
            string cs = cs0 + tenths % 100L;
            if (tenths > 3600000L) { main = "0:00."; cs = "00"; }
            string text = main + cs;
            int tw = Raylib.MeasureText(text, 14);
            Raylib.DrawText(text, ab - tw - 2, d - 18, 14, Color.Black);
        }

        // i.a(int,boolean) : progress / loading bar
        public void ProgressBar(int n2, bool bottomFull)
        {
            int n3 = bottomFull ? l : d;
            Raylib.DrawRectangle(1, n3 - 4, ab - 2, 3, Color.Black);
            int w = (int)((long)(ab - 4 << 16) * (long)n2 >> 16) >> 16;
            Raylib.DrawRectangle(2, n3 - 3, w, 1, Color.White);
        }

        // ===== input =====

        public void SetKeyset(int k) => keyset = k;

        // equivalent of i.xa(): collect held keys -> bike.Input
        public void PollGameInput(Bike bike)
        {
            int n3 = 0, n4 = 0;
            for (int digit = 0; digit < 10; ++digit)
            {
                if (DigitDown(digit))
                {
                    n3 += m[keyset][digit][0];
                    n4 += m[keyset][digit][1];
                }
            }
            // arrow keys -> game actions 1(UP),2(LEFT),5(RIGHT),6(DOWN)
            if (Raylib.IsKeyDown(KeyboardKey.Up)) { n3 += D[1][0]; n4 += D[1][1]; }
            if (Raylib.IsKeyDown(KeyboardKey.Left)) { n3 += D[2][0]; n4 += D[2][1]; }
            if (Raylib.IsKeyDown(KeyboardKey.Right)) { n3 += D[5][0]; n4 += D[5][1]; }
            if (Raylib.IsKeyDown(KeyboardKey.Down)) { n3 += D[6][0]; n4 += D[6][1]; }
            bike.Input(n3, n4);
        }

        private static bool DigitDown(int digit)
        {
            KeyboardKey top = (KeyboardKey)((int)KeyboardKey.Zero + digit);
            KeyboardKey kp = (KeyboardKey)((int)KeyboardKey.Kp0 + digit);
            return Raylib.IsKeyDown(top) || Raylib.IsKeyDown(kp);
        }

        public void Unload()
        {
            Raylib.UnloadTexture(helmet);
            Raylib.UnloadTexture(engine);
            Raylib.UnloadTexture(fender);
            Raylib.UnloadTexture(limb[0]);
            Raylib.UnloadTexture(limb[1]);
            Raylib.UnloadTexture(limb[2]);
            Raylib.UnloadTexture(sprites);
            Raylib.UnloadTexture(raster);
            Raylib.UnloadTexture(logo);
            Raylib.UnloadTexture(splash);
        }
    }
}
