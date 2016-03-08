using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace IO.Ably.Sandbox
{
    /// <summary>Utility class to unzip an embedded resource.</summary>
    internal static class Unzip
    {
        static Stream resourceStream( string localResName )
        {
            Assembly ass = typeof(Unzip).GetTypeInfo().Assembly;
            string defaultNamespace = ass.GetName().Name;
            string resName = String.Format("{0}.{1}",  defaultNamespace , localResName );
            Stream stm = ass.GetManifestResourceStream( resName );
            if( null == stm )
                throw new Exception( "Resource not found: " + resName );
            return stm;
        }

        public static byte[] resourceBytesGzip( string localResName )
        {
            using( Stream stm = resourceStream( localResName ) )
            {
                // The last 4 bytes of the .gz = uncompressed length
                // http://stackoverflow.com/a/4666324/126995
                stm.Seek( -4, SeekOrigin.End );
                byte[] buff = new byte[ 4 ];
                stm.Read( buff, 0, 4 );
                int length = BitConverter.ToInt32( buff, 0 );
                stm.Seek( 0, SeekOrigin.Begin );

                buff = new byte[ length ];
                using( GZipStream decompressed = new GZipStream( stm, CompressionMode.Decompress, false ) )
                {
                    if( length != decompressed.Read( buff, 0, length ) )
                        throw new EndOfStreamException();
                    return buff;
                }
            }
        }
    }
}