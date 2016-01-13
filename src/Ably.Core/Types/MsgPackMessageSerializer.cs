using MsgPack;
using System;
using System.Linq;

namespace Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        static readonly TypeMetadata meta;

        static void packArray<tElt>( Packer packer, TypeMetadata mdMessage, tElt[] arr )
        {
            packer.PackArrayHeader( arr.Length );
            foreach( var m in arr )
                mdMessage.serialize( m, packer );
        }

        static tElt[] unpackArray<tElt>( Unpacker unpacker, TypeMetadata mdMessage )
        {
            long ll;
            unpacker.ReadArrayLength( out ll );
            int l = (int)ll;
            tElt[] arr = new tElt[ l ];
            for( int i = 0; i < l; i++ )
            {
                arr[ i ] = (tElt)mdMessage.deserialize( unpacker );
            }
            return arr;
        }

        static MsgPackMessageSerializer()
        {
            meta = new TypeMetadata( typeof( ProtocolMessage ) );

            TypeMetadata mdMessage = new TypeMetadata( typeof( Message ) );
            TypeMetadata mdPresence = new TypeMetadata( typeof( PresenceMessage ) );

            meta.setCustom( "messages",
                ( obj, packer ) =>
                {
                    Message[] arr = ((ProtocolMessage)obj).messages.Where( m => !m.isEmpty() ).ToArray();
                    packArray( packer, mdMessage, arr );
                },
                ( unp, obj ) =>
                {
                    Message[] arr = unpackArray<Message>( unp, mdMessage );
                    ( (ProtocolMessage)obj ).messages = arr;
                } );

            meta.setCustom( "presence",
                ( obj, packer ) =>
                {
                    PresenceMessage[] arr = ((ProtocolMessage)obj).presence;
                    packArray( packer, mdPresence, arr );
                },
                ( unp, obj ) =>
                {
                    PresenceMessage[] arr = unpackArray<PresenceMessage>( unp, mdPresence );
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