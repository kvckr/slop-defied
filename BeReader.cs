namespace GravityDefied
{
    // Minimal big-endian reader matching Java's DataInputStream semantics over a byte[].
    internal sealed class BeReader
    {
        private readonly byte[] data;
        private int pos;

        public BeReader(byte[] data, int offset)
        {
            this.data = data;
            this.pos = offset;
        }

        public void Skip(int n) => pos += n;

        public sbyte ReadByte() => (sbyte)data[pos++];

        public int ReadUByte() => data[pos++];

        public short ReadShort()
        {
            int hi = data[pos++];
            int lo = data[pos++];
            return (short)((hi << 8) | lo);
        }

        public int ReadInt()
        {
            int b0 = data[pos++];
            int b1 = data[pos++];
            int b2 = data[pos++];
            int b3 = data[pos++];
            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }
    }
}
