namespace GravityDefied
{
    // Port of class 'd': 16.16 fixed-point trig/math used throughout the engine.
    // All arithmetic is transcribed exactly from the original bytecode.
    internal static class FpMath
    {
        public const int MaxValue = int.MaxValue;   // d.cfr_renamed_1
        public const int MinValue = int.MinValue;   // d.cfr_renamed_7

        public const int HalfPi = 102944;  // d.cfr_renamed_9  (PI/2 in 16.16)
        public const int TwoPi  = 411775;  // d.cfr_renamed_13 (2*PI)
        public const int Pi     = 205887;  // d.cfr_renamed_3  (PI)
        public const int One    = 65536;   // d.cfr_renamed_12 (1.0)

        private const int J = 64;
        private const int K = J << 16;

        private static readonly int[] Sintab =
        {
            0, 1608, 3215, 4821, 6423, 8022, 9616, 11204, 12785, 14359, 15923, 17479,
            19024, 20557, 22078, 23586, 25079, 26557, 28020, 29465, 30893, 32302, 33692,
            35061, 36409, 37736, 39039, 40319, 41575, 42806, 44011, 45189, 46340, 47464,
            48558, 49624, 50660, 51665, 52639, 53581, 54491, 55368, 56212, 57022, 57797,
            58538, 59243, 59913, 60547, 61144, 61705, 62228, 62714, 63162, 63571, 63943,
            64276, 64571, 64826, 65043, 65220, 65358, 65457, 65516
        };

        private static readonly int[] Atantab =
        {
            0, 1023, 2047, 3069, 4090, 5109, 6126, 7139, 8149, 9155, 10157, 11155, 12146,
            13133, 14113, 15087, 16054, 17015, 17967, 18912, 19849, 20778, 21698, 22610,
            23512, 24405, 25289, 26163, 27027, 27882, 28726, 29561, 30385, 31199, 32003,
            32796, 33579, 34352, 35114, 35866, 36608, 37339, 38060, 38771, 39471, 40161,
            40841, 41512, 42172, 42822, 43463, 44094, 44716, 45328, 45931, 46524, 47109,
            47684, 48251, 48809, 49358, 49899, 50431, 50955
        };

        // d.a(int,int): fixed-point multiply
        public static int Mul(int a, int b)
        {
            return (int)((long)a * (long)b >> 16);
        }

        // d.cfr_renamed_11(int,int): fixed-point divide
        public static int Div(int a, int b)
        {
            return (int)(((long)a << 32) / (long)b >> 16);
        }

        // d.cfr_renamed_10(int): sine
        public static int Sin(int n)
        {
            while (n >= Pi) n -= TwoPi;
            while (n <= -Pi) n += TwoPi;
            if (n >= HalfPi) n = Pi - n;
            if (n <= -HalfPi) n = -Pi - n;
            if (n < 0)
            {
                int idx = Div(Mul(-n, K), HalfPi);
                return -Sintab[idx >> 16];
            }
            int idx2 = Div(Mul(n, K), HalfPi);
            return Sintab[idx2 >> 16];
        }

        // d.cfr_renamed_11(int): cosine
        public static int Cos(int n)
        {
            return Sin(HalfPi - n);
        }

        // d.a(int): arctangent
        public static int Atan(int n)
        {
            int result = 0;
            bool neg = false;
            try
            {
                if (n < 0)
                {
                    neg = true;
                    n = -n;
                }
                if (n <= One)
                {
                    int t = Mul(n, K);
                    result = (t >> 16 == K) ? HalfPi : Atantab[t >> 16];
                }
                else
                {
                    int t = Mul(Div(One, n), K);
                    result = HalfPi - Atantab[t >> 16];
                }
            }
            catch (System.IndexOutOfRangeException)
            {
            }
            if (neg) return -result;
            return result;
        }

        // d.cfr_renamed_10(int,int): atan2
        public static int Atan2(int y, int x)
        {
            if ((x < 0 ? -x : x) < 3)
            {
                return (y > 0 ? 1 : -1) * HalfPi;
            }
            int a = Atan(Div(y, x));
            if (y > 0)
            {
                if (x > 0) return a;
                return Pi + a;
            }
            if (x > 0) return a;
            return a - Pi;
        }
    }
}
