namespace IO.Ably.Push
{
    internal static class PersistKeys
    {
        public static class StateMachine
        {
            public const string SharedName = "Ably_StateMachine";
            public const string CURRENT_STATE = "ABLY_PUSH_CURRENT_STATE";
            public const string PENDING_EVENTS_LENGTH = "ABLY_PUSH_PENDING_EVENTS_LENGTH";
            public const string PENDING_EVENTS = "ABLY_PUSH_PENDING_EVENTS";
            public const string PUSH_CUSTOM_REGISTRAR = "ABLY_PUSH_REGISTRATION_HANDLER";
        }

        public static class Device
        {
            public const string SharedName = "Ably_Device";

            public const string DEVICE_ID = "ABLY_DEVICE_ID";
            public const string CLIENT_ID = "ABLY_CLIENT_ID";
            public const string DEVICE_SECRET = "ABLY_DEVICE_SECRET";
            public const string DEVICE_TOKEN = "ABLY_DEVICE_IDENTITY_TOKEN";
            public const string TOKEN_TYPE = "ABLY_REGISTRATION_TOKEN_TYPE";
            public const string TOKEN = "ABLY_REGISTRATION_TOKEN";
        }
    }
}