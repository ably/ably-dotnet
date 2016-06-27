using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public class RealtimeTransportData
    {
        public ProtocolMessage Original { get; set; }
        public bool IsBinary => Length > 0;
        public byte[] Data { get; } = new byte[0];
        public string Text { get; }
        public int Length => Data.Length;

        public RealtimeTransportData(string text)
        {
            Text = text;
        }

        public RealtimeTransportData(byte[] data)
        {
            Data = data;
        }

        public string Explain()
        {
            if (IsBinary)
            {
                return $"Binary message with length: " + Length;
            }
            return Text;
        }
    }
}