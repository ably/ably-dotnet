using MsgPack;
using System;
using System.Collections.Generic;

namespace Ably.Types.MsgPack
{
    static class MsgPackUtils
    {
        public static object unpack( this MessagePackObject mp )
        {
            return ParseResult( mp );
        }

        static object ParseResult( MessagePackObject obj )
        {
            if( obj.IsList )
            {
                List<object> data = new List<object>();
                foreach( MessagePackObject objItem in obj.AsList() )
                {
                    data.Add( ParseResult( objItem ) );
                }
                return data.ToArray();
            }
            else if( obj.IsMap )
            {
                Dictionary< object ,object> data = new Dictionary< object ,object>();
                foreach( var objItem in obj.AsDictionary() )
                {
                    data.Add( ParseResult( objItem.Key ), ParseResult( objItem.Value ) );
                }
                return data;
            }
            else
            {
                if( obj.UnderlyingType != null && resolver.ContainsKey( obj.UnderlyingType ) )
                {
                    return resolver[ obj.UnderlyingType ]( obj );
                }
            }
            return null;
        }

        static readonly Dictionary<Type, Func<MessagePackObject, object>> resolver = new Dictionary<Type, Func<MessagePackObject, object>>()
        {
            { typeof(Byte), r => r.AsByte() },
            { typeof(SByte), r => r.AsSByte() },
            { typeof(Boolean), r => r.AsBoolean() },
            { typeof(UInt16), r => r.AsUInt16() },
            { typeof(UInt32), r => r.AsUInt32() },
            { typeof(UInt64), r => r.AsUInt64() },
            { typeof(Int16), r => r.AsInt16() },
            { typeof(Int32), r => r.AsInt32() },
            { typeof(Int64), r => r.AsInt64() },
            { typeof(Single), r => r.AsSingle() },
            { typeof(Double), r => r.AsDouble() },
            { typeof(String), r => r.AsStringUtf8() },
        };

        public static void packArray<tElt>( this Packer packer, TypeMetadata mdMessage, tElt[] arr )
        {
            packer.PackArrayHeader( arr.Length );
            foreach( var m in arr )
                mdMessage.serialize( m, packer );
        }

        public static tElt[] unpackArray<tElt>( this Unpacker unpacker, TypeMetadata mdMessage )
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


    }
}