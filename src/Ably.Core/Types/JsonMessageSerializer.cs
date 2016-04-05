using Newtonsoft.Json;

namespace IO.Ably.Types
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        static JsonMessageSerializer()
        {
            JsonConvert.DefaultSettings = Config.GetJsonSettings;
        }

        public ProtocolMessage DeserializeProtocolMessage( object value )
        {
            return JsonConvert.DeserializeObject<ProtocolMessage>( (string)value );
        }

        public object SerializeProtocolMessage( ProtocolMessage message )
        {
            return JsonConvert.SerializeObject( message );
        }
    }
}