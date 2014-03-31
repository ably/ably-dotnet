namespace Ably
{
    internal class TypedBuffer
    {
        public byte[] Buffer { get; set; }
        public Protocol.TType Type { get; set; }
    }
}