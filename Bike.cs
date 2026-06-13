namespace GravityDefied
{
    // Port of class 'b': the multi-body motorcycle physics simulation.
    // 6 rigid bodies (H) connected by 10 springs (Springs), integrated with a
    // midpoint scheme over an adaptive substep count, with terrain collision.
    // All arithmetic transcribed verbatim from the original 16.16 fixed-point code.
    internal sealed class Bike
    {
        // --- global engine parameters (were static in 'b') ---
        public static int Y;            // substep budget per Step()
        public static int Gravity;      // cfr_renamed_6
        public static int g, f, e;      // friction params
        public static int ae, ad;       // ground tangent friction
        public static int y;            // mass scale
        public static int q;            // wheel friction base
        public static int x;            // spring damping base
        public static int[] Radii;      // cfr_renamed_4 {114688,65536,32768}
        public static int P;            // max wheel omega
        public static int j;            // engine decay
        public static int Q;            // engine power max
        public static int EngTorqueStep;// cfr_renamed_8
        public static int ab;           // brake factor
        public static int W;
        public static int A;            // lean force
        public static int LeanLimit;    // cfr_renamed_2
        public static int h;            // current league/engine type

        // return codes
        public const int RUN = 0;       // a
        public const int GOAL1 = 1;     // V
        public const int GOAL2 = 2;     // B
        public const int CRASH = 3;     // ac
        public const int BACK = 4;      // cfr_renamed_13 (went back past start)
        public const int STUCK = 5;     // Z

        private int va = 0, wa = 1, xa = -1;
        public Body[] H;                // 6 bodies
        private Node[] Springs;         // 10 springs
        private int c = 0;              // engine torque accumulator
        private Levels l;
        private int E = 0, C = 0;       // collision normal
        private bool I = false;         // crashed (broken)
        private bool m = false;         // head/finish-touch crash
        private int T = 32768;          // suspension lean visual (0..65536, center 32768)
        private readonly int p;         // 3276
        private int k = 0;              // steer accumulator
        private bool v = false;         // auto/menu mode
        public bool b = false;
        private bool af = false;
        public Node[] aa = new Node[6]; // render snapshot
        private int t;
        public int z;
        public bool DriverSprite;       // cfr_renamed_5
        public bool BikeSprite;         // U
        private bool cAccel;            // d  (committed accelerate)
        private bool cBrake;            // F
        private bool cLeanBack;         // X
        private bool cLeanFwd;          // w
        private bool rAccel;            // cfr_renamed_10
        private bool rBrake;            // s
        private bool rLeanBack;         // O
        private bool rLeanFwd;          // r
        private bool R;                 // reached finish
        public bool N;                  // wheelie/finish-on-front flag
        private bool camSmooth;         // cfr_renamed_11
        private int o, n;               // camera smoothing offsets
        private int G;                  // camera max offset

        private readonly int[][] K, uc, S, wc, D, M;
        private int[][] J;

        public Bike(Levels levels)
        {
            this.p = 3276;
            for (int i = 0; i < 6; ++i) aa[i] = new Node();
            this.t = 0;
            this.z = 0;
            DriverSprite = false;
            BikeSprite = false;
            cAccel = false; cBrake = false; cLeanBack = false; cLeanFwd = false;
            rAccel = false; rBrake = false; rLeanBack = false; rLeanFwd = false;
            this.R = false;
            this.N = false;
            this.camSmooth = true;
            this.o = 0; this.n = 0;
            this.G = 655360;
            this.K = new int[][] { new[] { 183500, -52428 }, new[] { 262144, -163840 }, new[] { 406323, -65536 }, new[] { 445644, -39321 }, new[] { 235929, 39321 }, new[] { 16384, -144179 }, new[] { 13107, -78643 }, new[] { 288358, 81920 } };
            this.uc = new int[][] { new[] { 190054, -111411 }, new[] { 308019, -235929 }, new[] { 334233, -114688 }, new[] { 393216, -58982 }, new[] { 262144, 98304 }, new[] { 65536, -124518 }, new[] { 13107, -78643 }, new[] { 288358, 81920 } };
            this.S = new int[][] { new[] { 157286, 13107 }, new[] { 294912, -13107 }, new[] { 367001, 91750 }, new[] { 406323, 190054 }, new[] { 347340, 72089 }, new[] { 39321, -98304 }, new[] { 13107, -52428 }, new[] { 294912, 81920 } };
            this.wc = new int[][] { new[] { 183500, -39321 }, new[] { 262144, -131072 }, new[] { 393216, -65536 }, new[] { 458752, -39321 }, new[] { 294912, 6553 }, new[] { 16384, -144179 }, new[] { 13107, -78643 }, new[] { 288358, 85196 } };
            this.D = new int[][] { new[] { 190054, -91750 }, new[] { 255590, -235929 }, new[] { 334233, -114688 }, new[] { 393216, -42598 }, new[] { 301465, 6553 }, new[] { 65536, -78643 }, new[] { 13107, -78643 }, new[] { 288358, 85196 } };
            this.M = new int[][] { new[] { 157286, 13107 }, new[] { 294912, -13107 }, new[] { 367001, 104857 }, new[] { 406323, 176947 }, new[] { 347340, 72089 }, new[] { 39321, -98304 }, new[] { 13107, -52428 }, new[] { 288358, 85196 } };
            this.J = new int[][] { new[] { 45875 }, new[] { 32768 }, new[] { 52428 } };
            this.l = levels;
            ResetState(true);   // b ctor: cfr_renamed_11(true) builds the bodies
            this.v = false;
            SaveSnapshot();
            this.I = false;
        }

        // b.cfr_renamed_12() : current sprite-mode value
        public int SpriteMode()
        {
            if (DriverSprite && BikeSprite) return 3;
            if (BikeSprite) return 1;
            if (DriverSprite) return 2;
            return 0;
        }

        // b.cfr_renamed_11(int) : set sprite flags from bitmask
        public void SetSpriteFlags(int n)
        {
            DriverSprite = false;
            BikeSprite = false;
            if ((n & 2) != 0) DriverSprite = true;
            if ((n & 1) != 0) BikeSprite = true;
        }

        // b.cfr_renamed_12(int) : "setMode" — init gravity & engine
        public void SetMode(int n)
        {
            this.z = n;
            Y = 1310;
            Gravity = 0x190000;
            SetLeague(1);
            ResetState(true);
        }

        // b.cfr_renamed_7(int) : engine parameters per league
        public void SetLeague(int n)
        {
            h = n;
            g = 45875;
            f = 13107;
            e = 39321;
            y = 0x140000;
            x = 262144;
            j = 6553;
            switch (n)
            {
                case 3:
                    ae = 32768; ad = 32768; P = 0x160000; Q = 0x4B00000; EngTorqueStep = 0x360000;
                    ab = 6553; W = 26214; A = 65536; LeanLimit = 0x140000; q = 21626880;
                    break;
                case 2:
                    ae = 32768; ad = 32768; P = 0x140000; Q = 75366400; EngTorqueStep = 0x350000;
                    ab = 6553; W = 26214; A = 39321; LeanLimit = 327680; q = 21626880;
                    break;
                case 1:
                    ae = 32768; ad = 32768; P = 0x110000; Q = 65536000; EngTorqueStep = 0x320000;
                    ab = 6553; W = 26214; A = 26214; LeanLimit = 327680; q = 19660800;
                    break;
                default:
                    ae = 19660; ad = 19660; P = 0x110000; Q = 0x3200000; EngTorqueStep = 0x320000;
                    ab = 327; W = 0; A = 32768; LeanLimit = 327680; q = 19660800;
                    break;
            }
            ResetState(true);
        }

        // b.cfr_renamed_11(boolean) : reset to level start
        public void ResetState(bool bl)
        {
            this.t = 0;
            SetupBodies(l.LevelStartX(), l.LevelStartY());
            this.c = 0;
            this.k = 0;
            this.I = false;
            this.m = false;
            this.R = false;
            this.N = false;
            this.v = false;
            this.b = false;
            this.af = false;
            l.g.CamSet(H[2].S[5].Px + 98304 - Radii[0], H[1].S[5].Px - 98304 + Radii[0]);
        }

        // b.a(boolean) : nudge all bodies vertically (perspective compensation)
        public void ShiftY(bool bl)
        {
            int d = (bl ? 65536 : -65536) << 1;
            for (int i = 0; i < 6; ++i)
                for (int s = 0; s < 6; ++s)
                    H[i].S[s].Py += d;
        }

        // b.i(int,int) : build the bike at world (x,y)
        private void SetupBodies(int x0, int y0)
        {
            if (H == null) H = new Body[6];
            if (Springs == null) Springs = new Node[10];
            int type = 0, mass = 0, dx = 0, dy = 0;
            for (int i = 0; i < 6; ++i)
            {
                int inertia = 0;
                switch (i)
                {
                    case 0: type = 1; mass = 360448; dx = 0; dy = 0; break;
                    case 4: type = 1; mass = 229376; dx = -131072; dy = 196608; break;
                    case 3: type = 1; mass = 229376; dx = 131072; dy = 196608; break;
                    case 1: type = 0; mass = 98304; dx = 229376; dy = 0; break;
                    case 2: type = 0; mass = 360448; dx = -229376; dy = 0; inertia = 21626; break;
                    case 5: type = 2; mass = 294912; dx = 0; dy = 327680; break;
                }
                if (H[i] == null) H[i] = new Body();
                H[i].Reset();
                H[i].Radius = Radii[type];
                H[i].Type = type;
                H[i].InvMass = (int)((long)((int)(0x1000000000000L / (long)mass >> 16)) * (long)y >> 16);
                H[i].S[va].Px = x0 + dx;
                H[i].S[va].Py = y0 + dy;
                H[i].S[5].Px = x0 + dx;
                H[i].S[5].Py = y0 + dy;
                H[i].AngInertia = inertia;
            }
            for (int i = 0; i < 10; ++i)
            {
                if (Springs[i] == null) Springs[i] = new Node();
                Springs[i].Reset();
                Springs[i].Px = q;   // stiffness
                Springs[i].Ang = x;  // damping
            }
            // rest lengths (Springs[i].Py)
            Springs[0].Py = 229376;
            Springs[1].Py = 229376;
            Springs[2].Py = 236293;
            Springs[3].Py = 236293;
            Springs[4].Py = 262144;
            Springs[5].Py = 219814;
            Springs[6].Py = 219814;
            Springs[7].Py = 185363;
            Springs[8].Py = 185363;
            Springs[9].Py = 327680;
            Springs[5].Ang = (int)((long)x * 45875L >> 16);  // damping override
            Springs[6].Px = (int)(6553L * (long)q >> 16);
            Springs[5].Px = (int)(6553L * (long)q >> 16);
            Springs[9].Px = (int)(72089L * (long)q >> 16);
            Springs[8].Px = (int)(72089L * (long)q >> 16);
            Springs[7].Px = (int)(72089L * (long)q >> 16);
        }

        public void SetCamWindow(int n2, int n3) => l.CamWindow(n2, n3);

        // b.cfr_renamed_0()
        public void ClearInput()
        {
            rLeanBack = false; rLeanFwd = false; rBrake = false; rAccel = false;
        }

        // b.a(int,int) : steering/throttle input
        public void Input(int n2, int n3)
        {
            if (!v)
            {
                rLeanBack = false; rLeanFwd = false; rBrake = false; rAccel = false;
                if (n2 > 0) rAccel = true;
                else if (n2 < 0) rBrake = true;
                if (n3 > 0) { rLeanFwd = true; return; }
                if (n3 < 0) rLeanBack = true;
            }
        }

        public void Pause() { ResetState(true); v = true; }
        public void Unpause() { v = false; }
        public bool IsPaused() => v;

        // b.p() : auto-balance (used in menu/idle mode)
        private void AutoBalance()
        {
            int nx = H[1].S[va].Px - H[2].S[va].Px;
            int ny = H[1].S[va].Py - H[2].S[va].Py;
            int len = Len(nx, ny);
            int _ = (int)(((long)nx << 32) / (long)len >> 16);
            ny = (int)(((long)ny << 32) / (long)len >> 16);
            cBrake = false;
            if (ny < 0) { cLeanBack = true; cLeanFwd = false; }
            else if (ny > 0) { cLeanFwd = true; cLeanBack = false; }
            bool bl = (H[2].S[va].Py - H[0].S[va].Py > 0 ? 1 : -1) * (H[2].S[va].Vx - H[0].S[va].Vx > 0 ? 1 : -1) > 0;
            if ((bl && cLeanFwd) || (!bl && cLeanBack)) { cAccel = true; return; }
            cAccel = false;
        }

        // b.q() : apply engine torque & lean forces
        private void q_()
        {
            if (!I)
            {
                int nx = H[1].S[va].Px - H[2].S[va].Px;
                int ny = H[1].S[va].Py - H[2].S[va].Py;
                int len = Len(nx, ny);
                nx = (int)(((long)nx << 32) / (long)len >> 16);
                ny = (int)(((long)ny << 32) / (long)len >> 16);
                if (cAccel && c >= -Q) c -= EngTorqueStep;
                if (cBrake)
                {
                    c = 0;
                    H[1].S[va].Om = (int)((long)H[1].S[va].Om * (long)(65536 - ab) >> 16);
                    H[2].S[va].Om = (int)((long)H[2].S[va].Om * (long)(65536 - ab) >> 16);
                    if (H[1].S[va].Om < 6553) H[1].S[va].Om = 0;
                    if (H[2].S[va].Om < 6553) H[2].S[va].Om = 0;
                }
                H[0].InvMass = (int)(11915L * (long)y >> 16);
                H[0].InvMass = (int)(11915L * (long)y >> 16);
                H[4].InvMass = (int)(18724L * (long)y >> 16);
                H[3].InvMass = (int)(18724L * (long)y >> 16);
                H[1].InvMass = (int)(43690L * (long)y >> 16);
                H[2].InvMass = (int)(11915L * (long)y >> 16);
                H[5].InvMass = (int)(14563L * (long)y >> 16);
                if (cLeanBack)
                {
                    H[0].InvMass = (int)(18724L * (long)y >> 16);
                    H[4].InvMass = (int)(14563L * (long)y >> 16);
                    H[3].InvMass = (int)(18724L * (long)y >> 16);
                    H[1].InvMass = (int)(43690L * (long)y >> 16);
                    H[2].InvMass = (int)(10082L * (long)y >> 16);
                }
                else if (cLeanFwd)
                {
                    H[0].InvMass = (int)(18724L * (long)y >> 16);
                    H[4].InvMass = (int)(18724L * (long)y >> 16);
                    H[3].InvMass = (int)(14563L * (long)y >> 16);
                    H[1].InvMass = (int)(26214L * (long)y >> 16);
                    H[2].InvMass = (int)(11915L * (long)y >> 16);
                }
                if (cLeanBack || cLeanFwd)
                {
                    int n11 = -ny;
                    int n12 = nx;
                    int n5, n6, n7, n8, n9, n10;
                    if (cLeanBack && k > -LeanLimit)
                    {
                        n10 = 65536;
                        if (k < 0)
                            n10 = (int)(((long)(LeanLimit - (k < 0 ? -k : k)) << 32) / (long)LeanLimit >> 16);
                        n9 = (int)((long)A * (long)n10 >> 16);
                        n8 = (int)((long)n11 * (long)n9 >> 16);
                        n7 = (int)((long)n12 * (long)n9 >> 16);
                        n6 = (int)((long)nx * (long)n9 >> 16);
                        n5 = (int)((long)ny * (long)n9 >> 16);
                        T = T > 32768 ? (T - 1638 < 0 ? 0 : T - 1638) : (T - 3276 < 0 ? 0 : T - 3276);
                        H[4].S[va].Vx -= n8;
                        H[4].S[va].Vy -= n7;
                        H[3].S[va].Vx += n8;
                        H[3].S[va].Vy += n7;
                        H[5].S[va].Vx -= n6;
                        H[5].S[va].Vy -= n5;
                    }
                    if (cLeanFwd && k < LeanLimit)
                    {
                        n10 = 65536;
                        if (k > 0)
                            n10 = (int)(((long)(LeanLimit - k) << 32) / (long)LeanLimit >> 16);
                        n9 = (int)((long)A * (long)n10 >> 16);
                        n8 = (int)((long)n11 * (long)n9 >> 16);
                        n7 = (int)((long)n12 * (long)n9 >> 16);
                        n6 = (int)((long)nx * (long)n9 >> 16);
                        n5 = (int)((long)ny * (long)n9 >> 16);
                        T = T > 32768 ? (T + 1638 < 65536 ? T + 1638 : 65536) : (T + 3276 < 65536 ? T + 3276 : 65536);
                        H[4].S[va].Vx += n8;
                        H[4].S[va].Vy += n7;
                        H[3].S[va].Vx -= n8;
                        H[3].S[va].Vy -= n7;
                        H[5].S[va].Vx += n6;
                        H[5].S[va].Vy += n5;
                    }
                    return;
                }
                if (T < 26214) { T += 3276; return; }
                if (T > 39321) { T -= 3276; return; }
                T = 32768;
            }
        }

        // b.cfr_renamed_11() : the main physics step
        public int Step()
        {
            cAccel = rAccel;
            cBrake = rBrake;
            cLeanBack = rLeanBack;
            cLeanFwd = rLeanFwd;
            if (v) AutoBalance();
            Renderer.WindTick();
            q_();
            int n2 = u(Y);
            if (n2 == 5 || m) return 5;
            if (I) return 3;
            if (BelowFloor()) { N = false; return 4; }
            return n2;
        }

        // b.cfr_renamed_13() : went back past the start
        public bool BelowFloor() => H[1].S[va].Px < l.StartNodeX();

        // b.cfr_renamed_2() : crossed the finish line
        public bool CrossedFinish() => H[1].S[wa].Px > l.FinishX() || H[2].S[wa].Px > l.FinishX();

        // b.u(int) : adaptive substepping + collision resolution
        private int u(int n2)
        {
            bool bl = R;
            int n4 = 0;
            int n5 = n2;
            while (n4 < n2)
            {
                aa_step(n5 - n4);
                int n3;
                if (!bl && CrossedFinish()) n3 = 3;
                else n3 = ba(wa);
                if (!bl && R)
                {
                    if (n3 != 3) return 2;
                    return 1;
                }
                if (n3 == 0)
                {
                    if (((n5 = n4 + n5 >> 1) - n4 < 0 ? -(n5 - n4) : n5 - n4) >= 65) continue;
                    return 5;
                }
                if (n3 == 3)
                {
                    R = true;
                    n5 = n4 + n5 >> 1;
                    continue;
                }
                if (n3 == 1)
                {
                    int n6;
                    do
                    {
                        ca(wa);
                        n6 = ba(wa);
                        if (n6 == 0) return 5;
                    } while (n6 != 2);
                }
                n4 = n5;
                n5 = n2;
                va = va == 1 ? 0 : 1;
                wa = wa == 1 ? 0 : 1;
            }
            int dist = (int)((long)(H[1].S[va].Px - H[2].S[va].Px) * (long)(H[1].S[va].Px - H[2].S[va].Px) >> 16)
                     + (int)((long)(H[1].S[va].Py - H[2].S[va].Py) * (long)(H[1].S[va].Py - H[2].S[va].Py) >> 16);
            if (dist < 983040) I = true;
            if (dist > 0x460000) I = true;
            return 0;
        }

        // b.a(int) : accumulate forces into state n2
        private void accumForces(int n2)
        {
            for (int i = 0; i < 6; ++i)
            {
                Body k2 = H[i];
                Node n6 = k2.S[n2];
                k2.S[n2].Fx = 0;
                n6.Fy = 0;
                n6.Tq = 0;
                n6.Fy -= (int)(((long)Gravity << 32) / (long)k2.InvMass >> 16);
            }
            if (!I)
            {
                spring(H[0], Springs[1], H[2], n2, 65536);
                spring(H[0], Springs[0], H[1], n2, 65536);
                spring(H[2], Springs[6], H[4], n2, 131072);
                spring(H[1], Springs[5], H[3], n2, 131072);
            }
            spring(H[0], Springs[2], H[3], n2, 65536);
            spring(H[0], Springs[3], H[4], n2, 65536);
            spring(H[3], Springs[4], H[4], n2, 65536);
            spring(H[5], Springs[8], H[3], n2, 65536);
            spring(H[5], Springs[7], H[4], n2, 65536);
            spring(H[5], Springs[9], H[0], n2, 65536);

            Node wheel = H[2].S[n2];
            wheel.Tq = c = (int)((long)c * (long)(65536 - j) >> 16);
            if (wheel.Om > P) wheel.Om = P;
            if (wheel.Om < -P) wheel.Om = -P;

            int sumVx = 0, sumVy = 0;
            for (int i = 0; i < 6; ++i)
            {
                sumVx += H[i].S[n2].Vx;
                sumVy += H[i].S[n2].Vy;
            }
            sumVx = (int)(((long)sumVx << 32) / 393216L >> 16);
            sumVy = (int)(((long)sumVy << 32) / 393216L >> 16);
            int spd = 0;
            for (int i = 0; i < 6; ++i)
            {
                int rvx = H[i].S[n2].Vx - sumVx;
                int rvy = H[i].S[n2].Vy - sumVy;
                spd = Len(rvx, rvy);
                if (spd <= 0x1E0000) continue;
                int ux = (int)(((long)rvx << 32) / (long)spd >> 16);
                int uy = (int)(((long)rvy << 32) / (long)spd >> 16);
                H[i].S[n2].Vx -= ux;
                H[i].S[n2].Vy -= uy;
            }
            int s1 = H[2].S[n2].Py - H[0].S[n2].Py >= 0 ? 1 : -1;
            int s2 = H[2].S[n2].Vx - H[0].S[n2].Vx >= 0 ? 1 : -1;
            if (s1 * s2 > 0) { k = spd; return; }
            k = -spd;
        }

        // b.cfr_renamed_11(int,int) : fast vector length approximation
        public static int Len(int n2, int n3)
        {
            int an2 = n2 < 0 ? -n2 : n2;
            int an3 = n3 < 0 ? -n3 : n3;
            int hi, lo;
            if (an3 >= an2) { hi = an3; lo = an2; }
            else { hi = an2; lo = an3; }
            return (int)(64448L * (long)hi >> 16) + (int)(28224L * (long)lo >> 16);
        }

        // b.a(k,n,k,int,int) : spring force between body k2 and k3 via spring n2
        private void spring(Body k2, Node n2, Body k3, int slot, int factor)
        {
            Node n5 = k2.S[slot];
            Node n6 = k3.S[slot];
            int dx = n5.Px - n6.Px;
            int dy = n5.Py - n6.Py;
            int len = Len(dx, dy);
            if ((len < 0 ? -len : len) >= 3)
            {
                dx = (int)(((long)dx << 32) / (long)len >> 16);
                dy = (int)(((long)dy << 32) / (long)len >> 16);
                int stretch = len - n2.Py;                       // dist - rest
                int fSpring = (int)((long)stretch * (long)n2.Px >> 16); // * stiffness
                int fx = (int)((long)dx * (long)fSpring >> 16);
                int fy = (int)((long)dy * (long)fSpring >> 16);
                int rvx = n5.Vx - n6.Vx;
                int rvy = n5.Vy - n6.Vy;
                int damp = (int)((long)((int)((long)dx * (long)rvx >> 16) + (int)((long)dy * (long)rvy >> 16)) * (long)n2.Ang >> 16);
                fx += (int)((long)dx * (long)damp >> 16);
                fy += (int)((long)dy * (long)damp >> 16);
                fx = (int)((long)fx * (long)factor >> 16);
                fy = (int)((long)fy * (long)factor >> 16);
                n5.Fx -= fx;
                n5.Fy -= fy;
                n6.Fx += fx;
                n6.Fy += fy;
            }
        }

        // b.a(int,int,int) : derivative of state n2 into state n3, scaled by dt n4
        private void deriv(int n2, int n3, int dt)
        {
            for (int i = 0; i < 6; ++i)
            {
                Node src = H[i].S[n2];
                Node dst = H[i].S[n3];
                H[i].S[n3].Px = (int)((long)src.Vx * (long)dt >> 16);
                dst.Py = (int)((long)src.Vy * (long)dt >> 16);
                int n7 = (int)((long)dt * (long)H[i].InvMass >> 16);
                dst.Vx = (int)((long)src.Fx * (long)n7 >> 16);
                dst.Vy = (int)((long)src.Fy * (long)n7 >> 16);
            }
        }

        // b.z(int,int,int) : state n2 = base n3 + delta n4 / 2
        private void midpoint(int n2, int n3, int n4)
        {
            for (int i = 0; i < 6; ++i)
            {
                Node dst = H[i].S[n2];
                Node bas = H[i].S[n3];
                Node del = H[i].S[n4];
                dst.Px = bas.Px + (del.Px >> 1);
                dst.Py = bas.Py + (del.Py >> 1);
                dst.Vx = bas.Vx + (del.Vx >> 1);
                dst.Vy = bas.Vy + (del.Vy >> 1);
            }
        }

        // b.aa(int) : one integration substep of size n2
        private void aa_step(int n2)
        {
            accumForces(va);
            deriv(va, 2, n2);
            midpoint(4, va, 2);
            accumForces(4);
            deriv(4, 3, n2 >> 1);
            midpoint(4, va, 3);
            midpoint(wa, va, 2);
            midpoint(wa, wa, 3);
            for (int i = 1; i <= 2; ++i)
            {
                Node cur = H[i].S[va];
                Node nxt = H[i].S[wa];
                H[i].S[wa].Ang = cur.Ang + (int)((long)n2 * (long)cur.Om >> 16);
                nxt.Om = cur.Om + (int)((long)n2 * (long)((int)((long)H[i].AngInertia * (long)cur.Tq >> 16)) >> 16);
            }
        }

        // b.ba(int) : collision detection at state n2; returns 0/1/2
        private int ba(int n2)
        {
            int result = 2;
            int hi = H[1].S[n2].Px < H[2].S[n2].Px ? H[2].S[n2].Px : H[1].S[n2].Px;
            hi = hi < H[5].S[n2].Px ? H[5].S[n2].Px : hi;
            int lo = H[1].S[n2].Px < H[2].S[n2].Px ? H[1].S[n2].Px : H[2].S[n2].Px;
            lo = lo < H[5].S[n2].Px ? lo : H[5].S[n2].Px;
            l.LevelView(lo - Radii[0], hi + Radii[0], H[5].S[n2].Py);
            int nx = H[1].S[n2].Px - H[2].S[n2].Px;
            int ny = H[1].S[n2].Py - H[2].S[n2].Py;
            int len = Len(nx, ny);
            nx = (int)(((long)nx << 32) / (long)len >> 16);
            int ny2 = -((int)(((long)ny << 32) / (long)len >> 16));
            int nxN = nx;
            for (int i = 0; i < 6; ++i)
            {
                if (i == 4 || i == 3) continue;
                Node node = H[i].S[n2];
                if (i == 0)
                {
                    node.Px += (int)((long)ny2 * 65536L >> 16);
                    node.Py += (int)((long)nxN * 65536L >> 16);
                }
                int hit = l.Collide(node, H[i].Type);
                if (i == 0)
                {
                    node.Px -= (int)((long)ny2 * 65536L >> 16);
                    node.Py -= (int)((long)nxN * 65536L >> 16);
                }
                E = l.e;
                C = l.d;
                if (i == 5 && hit != 2) m = true;
                if (i == 1 && hit != 2) N = true;
                if (hit == 1) { xa = i; result = 1; continue; }
                if (hit != 0) continue;
                xa = i;
                result = 0;
                break;
            }
            return result;
        }

        // b.ca(int) : collision response on last-hit body
        private void ca(int n2)
        {
            Body k2 = H[xa];
            Node n8 = k2.S[n2];
            n8.Px += (int)((long)E * 3276L >> 16);
            n8.Py += (int)((long)C * 3276L >> 16);
            int n7, n6, n5, n4, n3;
            if (cBrake && (xa == 2 || xa == 1) && n8.Om < 6553)
            {
                n7 = g - W;
                n6 = 13107;
                n5 = 39321;
                n4 = 26214 - W;
                n3 = 26214 - W;
            }
            else
            {
                n7 = g;
                n6 = f;
                n5 = e;
                n4 = ae;
                n3 = ad;
            }
            int n9 = Len(E, C);
            E = (int)(((long)E << 32) / (long)n9 >> 16);
            C = (int)(((long)C << 32) / (long)n9 >> 16);
            int n10 = n8.Vx;
            int n11 = n8.Vy;
            int n12 = -((int)((long)n10 * (long)E >> 16) + (int)((long)n11 * (long)C >> 16));
            int n13 = -((int)((long)n10 * (long)(-C) >> 16) + (int)((long)n11 * (long)E >> 16));
            int n14 = (int)((long)n7 * (long)n8.Om >> 16) - (int)((long)n6 * (long)((int)(((long)n13 << 32) / (long)k2.Radius >> 16)) >> 16);
            int n15 = (int)((long)n4 * (long)n13 >> 16) - (int)((long)n5 * (long)((int)((long)n8.Om * (long)k2.Radius >> 16)) >> 16);
            int n16 = -((int)((long)n3 * (long)n12 >> 16));
            int n17 = (int)((long)(-n15) * (long)(-C) >> 16);
            int n18 = (int)((long)(-n15) * (long)E >> 16);
            int n19 = (int)((long)(-n16) * (long)E >> 16);
            int n20 = (int)((long)(-n16) * (long)C >> 16);
            n8.Om = n14;
            n8.Vx = n17 + n19;
            n8.Vy = n18 + n20;
        }

        public void SetCamSmooth(bool bl) => camSmooth = bl;

        // b.cfr_renamed_1(int) : configure camera max offset for screen size
        public void SetCamMax(int n2)
        {
            G = (int)(((long)((int)(655360L * (long)(n2 << 16) >> 16)) << 32) / 0x800000L >> 16);
        }

        // b.cfr_renamed_5() : camera x (pixels)
        public int CamX()
        {
            o = camSmooth ? (int)(((long)aa[0].Vx << 32) / 0x180000L >> 16) + (int)((long)o * 57344L >> 16) : 0;
            o = o < G ? o : G;
            o = o < -G ? -G : o;
            return aa[0].Px + o << 2 >> 16;
        }

        // b.cfr_renamed_10() : camera y (pixels)
        public int CamY()
        {
            n = camSmooth ? (int)(((long)aa[0].Vy << 32) / 0x180000L >> 16) + (int)((long)n * 57344L >> 16) : 0;
            n = n < G ? n : G;
            n = n < -G ? -G : n;
            return aa[0].Py + n << 2 >> 16;
        }

        // b.cfr_renamed_9() : terrain index under the bike (for progress)
        public int ProgressIndex()
        {
            int x = aa[1].Px < aa[2].Px ? aa[2].Px : aa[1].Px;
            if (I) return l.TerrainIndexAtX(aa[0].Px);
            return l.TerrainIndexAtX(x);
        }

        // b.cfr_renamed_8() : snapshot state va -> slot 5
        public void SaveSnapshot()
        {
            lock (H)
            {
                for (int i = 0; i < 6; ++i)
                {
                    H[i].S[5].Px = H[i].S[va].Px;
                    H[i].S[5].Py = H[i].S[va].Py;
                    H[i].S[5].Ang = H[i].S[va].Ang;
                }
                H[0].S[5].Vx = H[0].S[va].Vx;
                H[0].S[5].Vy = H[0].S[va].Vy;
                H[2].S[5].Om = H[2].S[va].Om;
            }
        }

        // b.cfr_renamed_6() : copy snapshot slot 5 -> render array
        public void PrepareRender()
        {
            lock (H)
            {
                for (int i = 0; i < 6; ++i)
                {
                    aa[i].Px = H[i].S[5].Px;
                    aa[i].Py = H[i].S[5].Py;
                    aa[i].Ang = H[i].S[5].Ang;
                }
                aa[0].Vx = H[0].S[5].Vx;
                aa[0].Vy = H[0].S[5].Vy;
                aa[2].Om = H[2].S[5].Om;
            }
        }

        // ===== rendering =====

        // b.a(i,int,int) : draw bike engine + fork sprites
        private void DrawBikeSprites(Renderer r, int n2, int n3)
        {
            int n4 = FpMath.Atan2(aa[0].Px - aa[3].Px, aa[0].Py - aa[3].Py);
            int n5 = FpMath.Atan2(aa[0].Px - aa[4].Px, aa[0].Py - aa[4].Py);
            int n6 = (aa[0].Px >> 1) + (aa[3].Px >> 1);
            int n7 = (aa[0].Py >> 1) + (aa[3].Py >> 1);
            int n8 = (aa[0].Px >> 1) + (aa[4].Px >> 1);
            int n9 = (aa[0].Py >> 1) + (aa[4].Py >> 1);
            int n10 = -n3;
            int n11 = n2;
            r.Wheel((n8 += (int)((long)n10 * 65536L >> 16) - (int)((long)n2 * 117964L >> 16)) << 2 >> 16, (n9 += (int)((long)n11 * 65536L >> 16) - (int)((long)n3 * 131072L >> 16)) << 2 >> 16, n5);
            r.Engine((n6 += (int)((long)n10 * 65536L >> 16) - (int)((long)n2 * 32768L >> 16)) << 2 >> 16, (n7 += (int)((long)n11 * 65536L >> 16) - (int)((long)n3 * 32768L >> 16)) << 2 >> 16, n4);
        }

        // b.la(i) : the chain line
        private void DrawChain(Renderer r)
        {
            r.SetColor(128, 128, 128);
            r.LineFx(aa[3].Px, aa[3].Py, aa[1].Px, aa[1].Py);
        }

        // b.a(i) : wheel circles
        private void DrawWheels(Renderer r)
        {
            int n2 = 1, n3 = 1;
            switch (h)
            {
                case 2: case 3: n3 = 0; n2 = 0; break;
                case 1: n2 = 0; break;
            }
            r.WheelIcon(aa[2].Px << 2 >> 16, aa[2].Py << 2 >> 16, n2);
            r.WheelIcon(aa[1].Px << 2 >> 16, aa[1].Py << 2 >> 16, n3);
        }

        // b.cfr_renamed_11(i) : rider spokes + wheel hubs
        private void DrawRider(Renderer r)
        {
            int n3 = H[1].Radius;
            int n4 = (int)((long)n3 * 58982L >> 16);
            int n5 = (int)((long)n3 * 45875L >> 16);
            r.SetColor(0, 0, 0);
            if (Game.InMenu)
            {
                int n6 = n3;
                r.Circle(aa[1].Px << 2 >> 16, aa[1].Py << 2 >> 16, n6 + n6 << 2 >> 16);
                int n7 = n4;
                r.Circle(aa[1].Px << 2 >> 16, aa[1].Py << 2 >> 16, n7 + n7 << 2 >> 16);
                int n8 = n3;
                r.Circle(aa[2].Px << 2 >> 16, aa[2].Py << 2 >> 16, n8 + n8 << 2 >> 16);
                int n9 = n5;
                r.Circle(aa[2].Px << 2 >> 16, aa[2].Py << 2 >> 16, n9 + n9 << 2 >> 16);
            }
            int n10 = n4;
            int n11 = 0;
            int n12 = aa[1].Ang;
            int n13 = FpMath.Cos(n12);
            int n14 = FpMath.Sin(n12);
            int n15 = n10;
            n10 = (int)((long)n13 * (long)n10 >> 16) + (int)((long)(-n14) * (long)n11 >> 16);
            n11 = (int)((long)n14 * (long)n15 >> 16) + (int)((long)n13 * (long)n11 >> 16);
            n12 = 82354;
            n13 = FpMath.Cos(82354);
            n14 = FpMath.Sin(n12);
            for (int s = 0; s < 5; ++s)
            {
                r.LineFx(aa[1].Px, aa[1].Py, aa[1].Px + n10, aa[1].Py + n11);
                n15 = n10;
                n10 = (int)((long)n13 * (long)n10 >> 16) + (int)((long)(-n14) * (long)n11 >> 16);
                n11 = (int)((long)n14 * (long)n15 >> 16) + (int)((long)n13 * (long)n11 >> 16);
            }
            n10 = n4;
            n11 = 0;
            n12 = aa[2].Ang;
            n13 = FpMath.Cos(n12);
            n14 = FpMath.Sin(n12);
            n15 = n10;
            n10 = (int)((long)n13 * (long)n10 >> 16) + (int)((long)(-n14) * (long)n11 >> 16);
            n11 = (int)((long)n14 * (long)n15 >> 16) + (int)((long)n13 * (long)n11 >> 16);
            n12 = 82354;
            n13 = FpMath.Cos(82354);
            n14 = FpMath.Sin(n12);
            for (int s = 0; s < 5; ++s)
            {
                r.LineFx(aa[2].Px, aa[2].Py, aa[2].Px + n10, aa[2].Py + n11);
                n15 = n10;
                n10 = (int)((long)n13 * (long)n10 >> 16) + (int)((long)(-n14) * (long)n11 >> 16);
                n11 = (int)((long)n14 * (long)n15 >> 16) + (int)((long)n13 * (long)n11 >> 16);
            }
            if (h > 0)
            {
                r.SetColor(255, 0, 0);
                if (h > 2) r.SetColor(100, 100, 255);
                r.Circle(aa[2].Px << 2 >> 16, aa[2].Py << 2 >> 16, 4);
                r.Circle(aa[1].Px << 2 >> 16, aa[1].Py << 2 >> 16, 4);
            }
        }

        // b.cfr_renamed_10(i,int,int,int,int) : draw chassis + driver
        private void DrawChassis(Renderer r, int n2, int n3, int n4, int n5)
        {
            int n7 = 0;
            int n8 = 65536;
            int n9 = aa[0].Px;
            int n10 = aa[0].Py;
            int n11 = 0, n12 = 0, n13 = 0, n14 = 0, n15 = 0, n16 = 0, n17 = 0, n18 = 0;
            int n19 = 0, n20 = 0, n21 = 0, n22 = 0, n23 = 0, n24 = 0, n25 = 0, n26 = 0;
            int[][] nArray = null, nArray2 = null, nArray3 = null;
            if (DriverSprite)
            {
                if (T < 32768) { nArray2 = uc; nArray3 = K; n8 = (int)((long)T * 131072L >> 16); }
                else if (T > 32768) { n7 = 1; nArray2 = K; nArray3 = S; n8 = (int)((long)(T - 32768) * 131072L >> 16); }
                else nArray = K;
            }
            else
            {
                if (T < 32768) { nArray2 = D; nArray3 = wc; n8 = (int)((long)T * 131072L >> 16); }
                else if (T > 32768) { n7 = 1; nArray2 = wc; nArray3 = M; n8 = (int)((long)(T - 32768) * 131072L >> 16); }
                else nArray = wc;
            }
            for (int idx = 0; idx < K.Length; ++idx)
            {
                int cx, cy;
                if (nArray2 != null)
                {
                    cx = (int)((long)nArray2[idx][0] * (long)(65536 - n8) >> 16) + (int)((long)nArray3[idx][0] * (long)n8 >> 16);
                    cy = (int)((long)nArray2[idx][1] * (long)(65536 - n8) >> 16) + (int)((long)nArray3[idx][1] * (long)n8 >> 16);
                }
                else { cx = nArray[idx][0]; cy = nArray[idx][1]; }
                int px = n9 + (int)((long)n4 * (long)cx >> 16) + (int)((long)n2 * (long)cy >> 16);
                int py = n10 + (int)((long)n5 * (long)cx >> 16) + (int)((long)n3 * (long)cy >> 16);
                switch (idx)
                {
                    case 0: n17 = px; n18 = py; break;
                    case 1: n19 = px; n20 = py; break;
                    case 2: n21 = px; n22 = py; break;
                    case 3: n23 = px; n24 = py; break;
                    case 4: n25 = px; n26 = py; break;
                    case 5: n13 = px; n14 = py; break;
                    case 6: n15 = px; n16 = py; break;
                    case 7: n11 = px; n12 = py; break;
                }
            }
            int n31 = (int)((long)J[n7][0] * (long)(65536 - n8) >> 16) + (int)((long)J[n7 + 1][0] * (long)n8 >> 16);
            if (DriverSprite)
            {
                r.Limb(n13 << 2, n14 << 2, n17 << 2, n18 << 2, 1);
                r.Limb(n17 << 2, n18 << 2, n19 << 2, n20 << 2, 1);
                r.Limb(n19 << 2, n20 << 2, n21 << 2, n22 << 2, 2, n31);
                r.Limb(n21 << 2, n22 << 2, n25 << 2, n26 << 2, 0);
                int ang = FpMath.Atan2(n2, n3);
                if (T > 32768) ang += 20588;
                r.Helmet(n23 << 2 >> 16, n24 << 2 >> 16, ang);
            }
            else
            {
                r.SetColor(0, 0, 0);
                r.LineFx(n13, n14, n17, n18);
                r.LineFx(n17, n18, n19, n20);
                r.SetColor(0, 0, 128);
                r.LineFx(n19, n20, n21, n22);
                r.LineFx(n21, n22, n25, n26);
                r.LineFx(n25, n26, n11, n12);
                int n6 = 65536;
                r.SetColor(156, 0, 0);
                int n32 = n6;
                r.Circle(n23 << 2 >> 16, n24 << 2 >> 16, n32 + n32 << 2 >> 16);
            }
            r.SetColor(0, 0, 0);
            r.Peg(n11 << 2 >> 16, n12 << 2 >> 16);
            r.Peg(n15 << 2 >> 16, n16 << 2 >> 16);
        }

        // b.a(i,int,int,int,int) : bike as line graphics (when bike sprite off)
        private void DrawBikeLines(Renderer r, int n2, int n3, int n4, int n5)
        {
            int n6 = aa[2].Px;
            int n7 = aa[2].Py;
            int n8 = n6 + (int)((long)n4 * (long)32768 >> 16);
            int n9 = n7 + (int)((long)n5 * (long)32768 >> 16);
            int n10 = n6 - (int)((long)n4 * (long)32768 >> 16);
            int n11 = n7 - (int)((long)n5 * (long)32768 >> 16);
            int n12 = aa[0].Px + (int)((long)n2 * 32768L >> 16);
            int n13 = aa[0].Py + (int)((long)n3 * 32768L >> 16);
            int n14 = n12 - (int)((long)n2 * 131072L >> 16);
            int n15 = n13 - (int)((long)n3 * 131072L >> 16);
            int n16 = n14 + (int)((long)n4 * 65536L >> 16);
            int n17 = n15 + (int)((long)n5 * 65536L >> 16);
            int n18 = n14 + (int)((long)n2 * 49152L >> 16) + (int)((long)n4 * 49152L >> 16);
            int n19 = n15 + (int)((long)n3 * 49152L >> 16) + (int)((long)n5 * 49152L >> 16);
            int n20 = n14 + (int)((long)n4 * 32768L >> 16);
            int n21 = n15 + (int)((long)n5 * 32768L >> 16);
            int n22 = aa[1].Px;
            int n23 = aa[1].Py;
            int n24 = aa[4].Px - (int)((long)n2 * 49152L >> 16);
            int n25 = aa[4].Py - (int)((long)n3 * 49152L >> 16);
            int n26 = n24 - (int)((long)n4 * 32768L >> 16);
            int n27 = n25 - (int)((long)n5 * 32768L >> 16);
            int n28 = n24 - (int)((long)n2 * 131072L >> 16) + (int)((long)n4 * 16384L >> 16);
            int n29 = n25 - (int)((long)n3 * 131072L >> 16) + (int)((long)n5 * 16384L >> 16);
            int n30 = aa[3].Px;
            int n31 = aa[3].Py;
            int n32 = n30 + (int)((long)n4 * 32768L >> 16);
            int n33 = n31 + (int)((long)n5 * 32768L >> 16);
            int n34 = n30 + (int)((long)n4 * 114688L >> 16) - (int)((long)n2 * 32768L >> 16);
            int n35 = n31 + (int)((long)n5 * 114688L >> 16) - (int)((long)n3 * 32768L >> 16);
            r.SetColor(50, 50, 50);
            r.Circle(n20 << 2 >> 16, n21 << 2 >> 16, 32768 + 32768 << 2 >> 16);
            if (!I)
            {
                r.LineFx(n8, n9, n16, n17);
                r.LineFx(n10, n11, n14, n15);
            }
            r.LineFx(n12, n13, n14, n15);
            r.LineFx(n12, n13, n30, n31);
            r.LineFx(n18, n19, n32, n33);
            r.LineFx(n32, n33, n34, n35);
            if (!I)
            {
                r.LineFx(n30, n31, n22, n23);
                r.LineFx(n34, n35, n22, n23);
            }
            r.LineFx(n16, n17, n26, n27);
            r.LineFx(n18, n19, n24, n25);
            r.LineFx(n24, n25, n28, n29);
            r.LineFx(n26, n27, n28, n29);
        }

        // b.cfr_renamed_10(i) : full render
        public void Render(Renderer r)
        {
            r.Clear();
            int n2 = aa[3].Px - aa[4].Px;
            int n3 = aa[3].Py - aa[4].Py;
            int n4 = Len(n2, n3);
            if (n4 != 0)
            {
                n2 = (int)(((long)n2 << 32) / (long)n4 >> 16);
                n3 = (int)(((long)n3 << 32) / (long)n4 >> 16);
            }
            int n5 = -n3;
            int n6 = n2;
            if (I)
            {
                int a3 = aa[3].Px;
                int a4 = aa[4].Px;
                if (a3 >= a4) { int t = a3; a3 = a4; a4 = t; }
                l.g.CamSet(a3, a4);
            }
            if (Levels.Perspective) l.DrawTerrainPersp(r, aa[0].Px, aa[0].Py);
            if (BikeSprite) DrawBikeSprites(r, n2, n3);
            if (!Game.InMenu) DrawWheels(r);
            DrawRider(r);
            if (BikeSprite) r.SetColor(170, 0, 0);
            else r.SetColor(50, 50, 50);
            r.WheelArc(aa[1].Px << 2 >> 16, aa[1].Py << 2 >> 16, Radii[0] << 2 >> 16, FpMath.Atan2(n2, n3));
            if (!I) DrawChain(r);
            DrawChassis(r, n2, n3, n5, n6);
            if (!BikeSprite) DrawBikeLines(r, n2, n3, n5, n6);
            l.DrawTerrain(r);
        }

        static Bike()
        {
            Radii = new int[] { 114688, 65536, 32768 };
            h = 0;
        }
    }
}
