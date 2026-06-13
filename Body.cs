namespace GravityDefied
{
    // Port of class 'k': a rigid body (bike part) holding 6 integration states.
    internal sealed class Body
    {
        public bool Active;       // cfr_renamed_11 ("do")
        public int Radius;        // a
        public int Type;          // cfr_renamed_7 (0,1,2 -> collision radius/render)
        public int InvMass;       // cfr_renamed_4 (inverse mass * scale)
        public int AngInertia;    // cfr_renamed_13 (angular inertia factor; non-zero for driven wheel)
        public Node[] S = new Node[6];   // cfr_renamed_10 : integration states

        public Body()
        {
            for (int i = 0; i < 6; ++i)
            {
                S[i] = new Node();
            }
            Reset();
        }

        // k.a()
        public void Reset()
        {
            AngInertia = 0;
            InvMass = 0;
            Radius = 0;
            Active = true;
            for (int i = 0; i < 6; ++i)
            {
                S[i].Reset();
            }
        }
    }
}
