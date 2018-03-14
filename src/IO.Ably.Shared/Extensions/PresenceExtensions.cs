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

            // if any part of the message serial fails to parse then throw an exception
            if (thisValues.Length != 3 ||
                !(int.TryParse(thisValues[1], out int msgSerialThis) | int.TryParse(thisValues[2], out int indexThis)))
            {
                throw new AblyException($"Parsing error. The Presence Message has an invalid Id '{thisMessage.Id}'.");
            }

            if (thatValues.Length != 3 ||
                !(int.TryParse(thatValues[1], out int msgSerialThat) | int.TryParse(thatValues[2], out int indexThat)))
            {
                throw new AblyException($"Parsing error. The Presence Message has an invalid Id '{thatMessage.Id}'.");
            }

            if (msgSerialThis == msgSerialThat)
            {
                return indexThis > indexThat;
            }

            return msgSerialThis > msgSerialThat;
        }
    }
}
