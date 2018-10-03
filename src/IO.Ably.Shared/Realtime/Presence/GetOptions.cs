using IO.Ably;

namespace IO.Ably.Realtime
{
    public class GetOptions
    {
        public bool WaitForSync { get; set; } = true;

        public string ClientId { get; set; }

        public string ConnectionId { get; set; }
    }
}
