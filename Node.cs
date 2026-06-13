namespace GravityDefied
{
    // Port of class 'n' in its physics role: a point-mass state.
    // The same field set is reused for springs (see Bike): for a spring
    //   Px = stiffness, Py = rest length, Ang = damping.
    internal sealed class Node
    {
        public int Px;    // cfr_renamed_8  : position x  (spring: stiffness)
        public int Py;    // i              : position y  (spring: rest length)
        public int Vx;    // e              : velocity x  (also derivative slot)
        public int Vy;    // d              : velocity y
        public int Fx;    // cfr_renamed_0  : force accumulator x
        public int Fy;    // cfr_renamed_2  : force accumulator y
        public int Ang;   // b              : wheel angle (spring: damping)
        public int Om;    // cfr_renamed_3  : angular velocity
        public int Tq;    // f              : torque accumulator

        public Node()
        {
            Reset();
        }

        // n.cfr_renamed_7()
        public void Reset()
        {
            Px = 0; Py = 0; Vx = 0;
            Fx = 0; Vy = 0; Ang = 0;
            Tq = 0; Om = 0; Fy = 0;
        }
    }
}
