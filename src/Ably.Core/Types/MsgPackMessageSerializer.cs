using MsgPack;
using System;
using System.Linq;

namespace Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        static readonly TypeMetadata meta;

        static MsgPackMessageSerializer()
        {
            meta = new TypeMetadata( typeof( ProtocolMessage ) );

            TypeMetadata mdMessage = new TypeMetadata( typeof( Message ) );

            mdMessage.remove( "data" );
            mdMessage.add( "data" );
            mdMessage.setCustom( "data",
                ( obj, packer ) =>
                {
                    object data = ((Message)obj).data;
                    if( data is byte[] )
                        packer.PackRaw( data as byte[] );
                    else
                        packer.PackString( data.ToString() );
                },
                ( unpacker, obj ) =>
                {
                    MessagePackObject result = unpacker.ReadItemData();
                    ( (Message)obj ).data = result.unpack();
                } );

            TypeMetadata mdPresence = new TypeMetadata( typeof( PresenceMessage ) );

            meta.setCustom( "messages",
                ( obj, packer ) =>
                {
                    Message[] arr = ((ProtocolMessage)obj).messages.Where( m => !m.isEmpty() ).ToArray();
                    packer.packArray( mdMessage, arr );
                },
                ( unp, obj ) =>
                {
                    Message[] arr = unp.unpackArray<Message>( mdMessage );
                    ( (ProtocolMessage)obj ).messages = arr;
                } );

            meta.setCustom( "presence",
                ( obj, packer ) =>
                {
                    PresenceMessage[] arr = ((ProtocolMessage)obj).presence;
                    packer.packArray( mdPresence, arr );
                },
                ( unp, obj ) =>
                {
                    PresenceMessage[] arr = unp.unpackArray<PresenceMessage>( mdPresence );
                    ( (ProtocolMessage)obj ).presence = arr;
                } );

            meta.setCustom( "flags",
                ( obj, packer ) => { throw new NotSupportedException(); },
                ( unp, obj ) =>
                {
                    int i;
                    unp.ReadInt32( out i );
                    ProtocolMessage.MessageFlag flags = (ProtocolMessage.MessageFlag)(byte)(i);
                    ( (ProtocolMessage)obj ).flags = flags;
                } );
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