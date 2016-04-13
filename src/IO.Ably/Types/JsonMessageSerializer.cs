using System.IO;
using Newtonsoft.Json;

namespace IO.Ably.Types
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public ProtocolMessage DeserializeProtocolMessage( object value )
        {
            return JsonConvert.DeserializeObject<ProtocolMessage>((string) value, Config.GetJsonSettings());
        }

        public object SerializeProtocolMessage( ProtocolMessage message )
        {
            return JsonConvert.SerializeObject( message, Config.GetJsonSettings() );
        }
    }
}