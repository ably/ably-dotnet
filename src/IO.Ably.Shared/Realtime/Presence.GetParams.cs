namespace IO.Ably.Realtime
{
    public partial class Presence
    {
        public class GetParams
        {
            public bool WaitForSync { get; set; } = true;

            public string ClientId { get; set; }

            public string ConnectionId { get; set; }
        }
    }
}
