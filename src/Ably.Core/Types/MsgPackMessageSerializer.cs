namespace Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        static readonly TypeMetadata meta;

        static MsgPackMessageSerializer()
        {
            meta = new TypeMetadata( typeof( ProtocolMessage ) );
        }

        public ProtocolMessage DeserializeProtocolMessage( object value )
        {
            return (ProtocolMessage)meta.deserialize( (byte[])value );
        }

        public object SerializeProtocolMessage( ProtocolMessage message )
        {
            return meta.serialize( message );
        }
    }
}