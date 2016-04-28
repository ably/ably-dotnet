using MsgPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IO.Ably.Types.MsgPack
{
    class TypeMetadata
    {
        readonly List<PropertyMetadata> list = new List<PropertyMetadata>();
        readonly Dictionary<string, PropertyMetadata> dict = new Dictionary<string, PropertyMetadata>();

        public readonly Type type;

        public TypeMetadata( Type tp )
        {
            this.type = tp;
            List<PropertyMetadata> props = new List<PropertyMetadata>();
            // Enum the properties
            foreach( PropertyInfo pi in tp.GetRuntimeProperties() )
            {
                if( pi.IsSpecialName )
                    continue;
                MethodInfo m  = pi.GetMethod ?? pi.SetMethod;
                if( m.IsStatic )
                    continue;
                if( !m.IsPublic )
                    continue;

                if( null != pi.GetCustomAttribute<JsonIgnoreAttribute>() )
                    continue;

                PropertyMetadata pm = new PropertyMetadata( pi );
                list.Add( pm );
                dict.Add( pm.name, pm );
            }
        }

        public void remove( string n )
        {
            PropertyMetadata pm = dict[ n ];
            dict.Remove( n );
            list.Remove( pm );
        }

        public void add( string clrPropName, string desializeName = null )
        {
            PropertyInfo pi = type.GetRuntimeProperty( clrPropName );
            PropertyMetadata pm = new PropertyMetadata( pi, desializeName );
            list.Add( pm );
            dict.Add( pm.name, pm );
        }

        public void setCustom( string name, Action<object, Packer> serialize, Action<Unpacker, object> deserialize, Func<object, bool> shouldSerialize = null)
        {
            PropertyMetadata meta = dict[ name ];
            meta.serialize = serialize;
            meta.deserialize = deserialize;
            if( null != shouldSerialize )
                meta.shouldSerializeMethod = shouldSerialize;
        }

        public byte[] serialize( object val )
        {
            PropertyMetadata[] props = list.Where( pm => pm.shouldSerialize( val ) ).ToArray();

            using( MemoryStream stream = new MemoryStream() )
            {
                using( Packer packer = Packer.Create( stream ) )
                {
                    this.serialize( val, packer );
                }
                return stream.ToArray();
            }
        }

        public void serialize( object val, Packer packer )
        {
            PropertyMetadata[] props = list.Where( pm => pm.shouldSerialize( val ) ).ToArray();
            packer.PackMapHeader( props.Length );
            foreach( PropertyMetadata meta in props )
            {
                packer.PackString( meta.name );
                meta.serialize( val, packer );
            }
        }

        public object deserialize( Unpacker unpacker )
        {
            object res = Activator.CreateInstance( type );

            long fieldCount = 0;
            unpacker.ReadMapLength( out fieldCount );
            for( int i = 0; i < fieldCount; i++ )
            {
                string propName;
                unpacker.ReadString( out propName );
                PropertyMetadata meta;
                if( !dict.TryGetValue( propName, out meta ) )
                {
                    Logger.Warning( "Deserializing {0}: unknown property {1}", this.type.FullName, propName );
                    MessagePackObject obj;
                    unpacker.ReadObject( out obj );
                    continue;
                }

                if( null == meta.deserialize )
                {
                    Logger.Error( "Deserializing {0}: no deserializer for property {1}", this.type.FullName, propName );
                    MessagePackObject obj;
                    unpacker.ReadObject( out obj );
                    continue;
                }

                meta.deserialize( unpacker, res );
            }
            return res;
        }

        public object deserialize( byte[] bytes )
        {
            using( MemoryStream stream = new MemoryStream( bytes ) )
            using( Unpacker unpacker = Unpacker.Create( stream ) )
            {
                return this.deserialize( unpacker );
            }
        }
    }
}