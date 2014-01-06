namespace Ably
{
    public class PresenceMessage
    {

        /**
         * Presence ActionType: the event signified by a PresenceMessage
         */
        public enum ActionType
        {
            Enter = 0,
            Leave = 1,
            Update = 2
        }


        public ActionType Action { get; set; }

        /**
         * The clientId associated with this presence action.
         */
        public string ClientId { get; set; }

        /**
         * A unique member identifier, disambiguating situations where a given
         * clientId is present on multiple connections simultaneously.
         */
        public string MemberId { get; set; }

        /**
         * Optional client-defined status or other event payload associated with this action.
         */
        public object ClientData { get; set; }

        public PresenceMessage()
        {
        }

        public PresenceMessage(ActionType action, string clientId)
            : this(action, clientId, null)
        {
        }

        public PresenceMessage(ActionType action, string clientId, object clientData)
        {
            Action = action;
            ClientId = clientId;
            ClientData = clientData;
        }
    }
}