namespace IO.Ably
{
    internal static class PresenceExtensions
    {
        public static bool IsSynthesized(this PresenceMessage msg)
        {
            return !msg.Id.StartsWith(msg.ConnectionId);
        }

        public static bool IsNewerThan(this PresenceMessage oldMessage, PresenceMessage newMessage)
        {
            return oldMessage.CompareTo(newMessage) > 0;
        }
    }
}