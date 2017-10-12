namespace IO.Ably
{
    internal static class PresenceExtensions
    {
        public static bool IsSynthesized(this PresenceMessage msg)
        {
            return !msg.Id.StartsWith(msg.ConnectionId);
        }

        public static bool IsNewerThan(this PresenceMessage thisMessage, PresenceMessage thatMessage)
        {
            if (thisMessage.IsSynthesized() || thatMessage.IsSynthesized())
            {
                return thisMessage.Timestamp > thatMessage.Timestamp;
            }
            
            var thisValues = thisMessage.Id.Split(':');
            var thatValues = thatMessage.Id.Split(':');
            var msgSerialThis = int.Parse(thisValues[1]);
            var msgSerialThat = int.Parse(thatValues[1]);
            var indexThis = int.Parse(thisValues[2]);
            var indexThat = int.Parse(thatValues[2]);

            if (msgSerialThis == msgSerialThat)
            {
                return indexThis > indexThat;
            }
            
            return msgSerialThis > msgSerialThat;
        }
    }
}