using System;
using System.Linq;
using System.Net;
using System.Text;
using Ably;
using Ably.Protocol;
using Newtonsoft.Json.Linq;
using TType = Thrift.Protocol.TType;
using System.Security.Cryptography;

namespace Ably
{
    internal sealed class CipherData : TypedBuffer
    {
        public CipherData(byte[] cipherText, Ably.Protocol.TType type)
        {
            Buffer = cipherText;
            Type = type;
        }

        public CipherData(byte[] cipherText, int type)
            : this(cipherText, (Protocol.TType)type)
        {
        }
    }

    public class CipherParams
    {
        public String Algorithm;
        public byte[] Key { get; set; }
    }

    public class Crypto
    {

        public const String DefaultAlgorithm = "AES";
        public const int DefaultKeylength = 128; // bits
        public const int DefaultBlocklength = 16; // bytes

        public static CipherParams GetDefaultParams()
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = DefaultKeylength;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.BlockSize = DefaultBlocklength;
                aes.GenerateKey();
                return new CipherParams() { Algorithm = DefaultAlgorithm, Key = aes.Key };
            }
        }

        public static CipherParams GetDefaultParams(byte[] key)
        {
            return new CipherParams() { Algorithm = "AES", Key = key };
        }

        public static IChannelCipher GetCipher(ChannelOptions opts)
        {
            CipherParams @params = opts.CipherParams ?? GetDefaultParams();

            if (string.Equals(@params.Algorithm, "aes", StringComparison.CurrentCultureIgnoreCase))
                return new AesCipher(@params);

            throw new AblyException("Currently only the AES encryption algorith is supported", 50000, HttpStatusCode.InternalServerError);
        }
    }

    public interface IChannelCipher
    {
        byte[] Encrypt(byte[] input);
        byte[] Decrypt(byte[] input);
    }

    public class ChannelOptions
    {
        public bool Encrypted { get; set; }
        public CipherParams CipherParams { get; set; }
    }

    internal class TypedBuffer
    {
        public byte[] Buffer { get; set; }
        public Protocol.TType Type { get; set; }
    }


    public class Data
    {
        internal static Object FromThrift(TData data)
        {
            Object result = null;
            Protocol.TType type = data.Type;

            switch (data.Type)
            {
                case Protocol.TType.NONE:
                    break;
                case Protocol.TType.TRUE:
                    result = true;
                    break;
                case Protocol.TType.FALSE:
                    result = false;
                    break;
                case Protocol.TType.INT32:
                    result = data.I32Data;
                    break;
                case Protocol.TType.INT64:
                    result = data.I64Data;
                    break;
                case Protocol.TType.DOUBLE:
                    result = data.DoubleData;
                    break;
                case Protocol.TType.STRING:
                    result = data.StringData;
                    break;
                case Protocol.TType.BUFFER:
                    byte[] extract = new byte[data.BinaryData.Count()];
                    data.BinaryData.CopyTo(extract, 0);
                    result = extract;
                    break;
                case Protocol.TType.JSONARRAY:
                    result = JArray.Parse(data.StringData);
                    break;
                case Protocol.TType.JSONOBJECT:
                    result = JObject.Parse(data.StringData);
                    break;
                default:
                    break;
            }
            return result;
        }

        internal static TData ToThrift(Object obj)
        {
            var result = new TData();
            if (obj is string)
            {
                result.Type = Ably.Protocol.TType.STRING;
                result.StringData = (string)obj;
            }
            else if (obj is byte[])
            {
                result.Type = (Ably.Protocol.TType.BUFFER);
                result.BinaryData = ((byte[])obj);
            }
            else if (obj is JObject)
            {
                result.Type = Protocol.TType.JSONOBJECT;
                result.StringData = obj.ToString();
            }
            else if (obj is JArray)
            {
                result.Type = Protocol.TType.JSONARRAY;
                result.StringData = obj.ToString();
            }
            else if (obj is bool)
            {
                result.Type = (bool)obj ? Protocol.TType.TRUE : Protocol.TType.FALSE;
            }
            else if (obj is int)
            {
                result.Type = (Protocol.TType.INT32);
                result.I32Data = (int)obj;
            }
            else if (obj is long)
            {
                result.Type = (Protocol.TType.INT64);
                result.I64Data = (long)obj;
            }
            else if (obj is Double)
            {
                result.Type = (Protocol.TType.DOUBLE);
                result.DoubleData = (double)obj;
            }
            else
            {
                throw new AblyException("Unsupported type: " + obj.GetType().FullName, 40000, HttpStatusCode.BadRequest);
            }
            return result;
        }

        internal static TypedBuffer AsPlaintext(Object obj)
        {
            var result = new TypedBuffer();
            if (obj is String)
            {
                result.Buffer = Encoding.UTF8.GetBytes((String)obj);
                result.Type = Protocol.TType.STRING;
            }
            else if (obj is byte[])
            {
                result.Buffer = (byte[])obj;
                result.Type = Protocol.TType.BUFFER;
            }
            else if (obj is JObject)
            {
                result.Buffer = Encoding.UTF8.GetBytes(obj.ToString());
                result.Type = Protocol.TType.JSONOBJECT;
            }
            else if (obj is JArray)
            {
                result.Buffer = Encoding.UTF8.GetBytes(obj.ToString());
                result.Type = Protocol.TType.JSONARRAY;
            }
            else if (obj is bool)
            {
                result = null;
            }
            else if (obj is int)
            {
                byte[] buffer = result.Buffer = new byte[4];
                int i32 = (int)obj;
                buffer[0] = (byte)(0xff & (i32 >> 24));
                buffer[1] = (byte)(0xff & (i32 >> 16));
                buffer[2] = (byte)(0xff & (i32 >> 8));
                buffer[3] = (byte)(0xff & (i32));
                result.Type = Protocol.TType.INT32;
            }
            else if (obj is long)
            {
                byte[] buffer = result.Buffer = new byte[8];
                long i64 = ((long)obj);
                buffer[0] = (byte)(0xff & (i64 >> 56));
                buffer[1] = (byte)(0xff & (i64 >> 48));
                buffer[2] = (byte)(0xff & (i64 >> 40));
                buffer[3] = (byte)(0xff & (i64 >> 32));
                buffer[4] = (byte)(0xff & (i64 >> 24));
                buffer[5] = (byte)(0xff & (i64 >> 16));
                buffer[6] = (byte)(0xff & (i64 >> 8));
                buffer[7] = (byte)(0xff & (i64));
                result.Type = Protocol.TType.INT64;
            }
            else if (obj is Double)
            {
                byte[] buffer = result.Buffer = new byte[8];
                long i64 = BitConverter.DoubleToInt64Bits((Double)obj);
                buffer[0] = (byte)(0xff & (i64 >> 56));
                buffer[1] = (byte)(0xff & (i64 >> 48));
                buffer[2] = (byte)(0xff & (i64 >> 40));
                buffer[3] = (byte)(0xff & (i64 >> 32));
                buffer[4] = (byte)(0xff & (i64 >> 24));
                buffer[5] = (byte)(0xff & (i64 >> 16));
                buffer[6] = (byte)(0xff & (i64 >> 8));
                buffer[7] = (byte)(0xff & (i64));
                result.Type = Protocol.TType.DOUBLE;
            }
            else
            {
                throw new AblyException("Unsupported type: " + obj.GetType().FullName, 40000, HttpStatusCode.BadRequest);
            }
            return result;
        }

        internal static Object FromPlaintext(TypedBuffer plaintext)
        {
            return FromPlaintext(plaintext.Buffer, plaintext.Type);
        }

        internal static Object FromPlaintext(byte[] plaintext, Protocol.TType baseType)
        {
            return FromPlaintext(plaintext, 0, plaintext.Length, baseType);
        }

        internal static Object FromPlaintext(byte[] plaintext, int offset, int length, Ably.Protocol.TType baseType)
        {
            Object result = null;
            switch (baseType)
            {
                case Protocol.TType.TRUE:
                    result = true;
                    break;
                case Protocol.TType.FALSE:
                    result = false;
                    break;
                case Protocol.TType.INT32:
                    int i32 =
                        ((plaintext[offset] & 0xff) << 24) |
                        ((plaintext[offset + 1] & 0xff) << 16) |
                        ((plaintext[offset + 2] & 0xff) << 8) |
                        ((plaintext[offset + 3] & 0xff));
                    result = i32;
                    break;
                case Protocol.TType.INT64:
                    long i64 =
                        (((long)(plaintext[offset] & 0xff)) << 56) |
                        (((long)(plaintext[offset + 1] & 0xff)) << 48) |
                        (((long)(plaintext[offset + 2] & 0xff)) << 40) |
                        (((long)(plaintext[offset + 3] & 0xff)) << 32) |
                        (((long)(plaintext[offset + 4] & 0xff)) << 24) |
                        (((long)(plaintext[offset + 5] & 0xff)) << 16) |
                        (((long)(plaintext[offset + 6] & 0xff)) << 8) |
                        ((long)(plaintext[offset + 7] & 0xff));
                    result = i64;
                    break;
                case Protocol.TType.DOUBLE:
                    long d64 =
                        (((long)(plaintext[offset] & 0xff)) << 56) |
                        (((long)(plaintext[offset + 1] & 0xff)) << 48) |
                        (((long)(plaintext[offset + 2] & 0xff)) << 40) |
                        (((long)(plaintext[offset + 3] & 0xff)) << 32) |
                        (((long)(plaintext[offset + 4] & 0xff)) << 24) |
                        (((long)(plaintext[offset + 5] & 0xff)) << 16) |
                        (((long)(plaintext[offset + 6] & 0xff)) << 8) |
                        ((long)(plaintext[offset + 7] & 0xff));
                    result = BitConverter.Int64BitsToDouble(d64);
                    break;
                case Protocol.TType.STRING:
                    result = Encoding.UTF8.GetString(plaintext, offset, length);
                    break;
                case Protocol.TType.BUFFER:
                    if (offset == 0 && length == plaintext.Length)
                        result = plaintext;
                    else
                    {
                        var resultArray = new byte[length];
                        Array.Copy(plaintext, offset, resultArray, 0, length);
                        result = resultArray;
                    }
                    break;
                case Protocol.TType.JSONOBJECT:
                    try
                    {
                        var jsonText = Encoding.UTF8.GetString(plaintext, offset, length);
                        result = JObject.Parse(jsonText);
                    }
                    catch (Exception e)
                    {
                        throw new AblyException(e);
                    }
                    break;
                case Protocol.TType.JSONARRAY:
                    try
                    {
                        var jsonText = Encoding.UTF8.GetString(plaintext, offset, length);

                        result = JArray.Parse(jsonText);
                    }
                    catch (Exception e) { throw new AblyException(e); }
                    break;
            }
            return result;
        }
    }
}