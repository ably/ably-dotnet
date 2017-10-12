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
            if (oldMessage.IsSynthesized() || newMessage.IsSynthesized())
            {
                if (oldMessage.Timestamp > newMessage.Timestamp) return true;
            }
            
            var thisValues = oldMessage.Id.Split(':');
            var otherValues = newMessage.Id.Split(':');
            var msgSerialThis = int.Parse(thisValues[1]);
            var msgSerialOther = int.Parse(otherValues[1]);
            var indexThis = int.Parse(thisValues[2]);
            var indexOther = int.Parse(otherValues[2]);

            if (msgSerialThis == msgSerialOther)
            {
                if (indexThis > indexOther) return true;
            }
            
            if (msgSerialThis > msgSerialOther) return true;
        }
    }
}