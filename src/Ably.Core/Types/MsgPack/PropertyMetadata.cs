using MsgPack;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace Ably.Types.MsgPack
{
    internal class PropertyMetadata
    {
        readonly PropertyInfo pi;
        public readonly string name;
        public Func<object, bool> shouldSerializeMethod;
        readonly bool canRead, canWrite;

        public Action<Unpacker, object> deserialize;
        public Action<object, Packer> serialize;

        public PropertyMetadata( PropertyInfo pi, string serializeName = null )
        {
            this.pi = pi;
            string propName = pi.Name;
            this.name = propName;

            var getter = pi.GetMethod;
            canRead = ( null != getter ) && getter.IsPublic;

            var setter = pi.SetMethod;
            canWrite = ( null != setter ) && setter.IsPublic;

            // Find prop name
            var jp = this.pi.GetCustomAttribute<JsonPropertyAttribute>();
            if( null != jp && jp.PropertyName.notEmpty() )
                this.name = jp.PropertyName;

            if( null != serializeName )
                this.name = serializeName;

            // Look for ShouldSerializeXxx method
            string ssm = "ShouldSerialize" + propName;
            var mi = this.pi.DeclaringType.GetRuntimeMethod(ssm, new Type[ 0 ] );
            if( null != mi )
                this.shouldSerializeMethod = ( obj ) => (bool)mi.Invoke( obj, null );

            // Fill those delegates
            if( pi.PropertyType == typeof( int ) || pi.PropertyType.GetTypeInfo().IsEnum )
            {
                this.deserialize = ( unp, obj ) =>
                {
                    int i;
                    unp.ReadInt32( out i );
                    pi.SetValue( obj, i );
                };
                this.serialize = ( obj, p ) => p.Pack( (int)pi.GetValue( obj ) );
                return;
            }
            if( pi.PropertyType == typeof( long ) )
            {
                this.deserialize = ( unp, obj ) =>
                {
                    long i;
                    unp.ReadInt64( out i );
                    pi.SetValue( obj, i );
                };
                this.serialize = ( obj, p ) => p.Pack( (long)pi.GetValue( obj ) );
                return;
            }
            if( pi.PropertyType == typeof( long? ) )
            {
                this.deserialize = ( unp, obj ) =>
                {
                    long? i;
                    unp.ReadNullableInt64( out i );
                    pi.SetValue( obj, i );
                };
                this.serialize = ( obj, p ) => p.Pack( (long?)pi.GetValue( obj ) );
                return;
            }
            if( pi.PropertyType == typeof( string ) )
            {
                this.deserialize = ( unp, obj ) =>
                {
                    string s;
                    unp.ReadString( out s );
                    pi.SetValue( obj, s );
                };
                this.serialize = ( obj, p ) => p.Pack( (string)pi.GetValue( obj ) );
                return;
            }
        }

        public bool shouldSerialize( object obj )
        {
            if( !this.canRead )
                return false;
            if( null == this.serialize )
                return false;

            if( null != this.shouldSerializeMethod )
                if( !this.shouldSerializeMethod( obj ) )
                    return false;

            object val = this.pi.GetValue(obj);
            if( null == val )
                return false;

            if( val is string && ( (string)val ) == "" )
                return false;

            return true;
        }
    }
}