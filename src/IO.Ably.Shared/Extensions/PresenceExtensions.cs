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

            // if there are not 3 elements then return false
            if (thisValues.Length != 3 || thatValues.Length != 3) return false;

            // if any part of the message serial fails to parse then exit returning false
            if (!(int.TryParse(thisValues[1], out int msgSerialThis) |
                  int.TryParse(thatValues[1], out int msgSerialThat) |
                  int.TryParse(thisValues[2], out int indexThis) |
                  int.TryParse(thatValues[2], out int indexThat))) return false;

            if (msgSerialThis == msgSerialThat)
            {
                return indexThis > indexThat;
            }
            return msgSerialThis > msgSerialThat;
        }
    }
}