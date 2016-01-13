using System;
using Ably.Encryption;
using Ably.Rest;

namespace Ably.MessageEncoders
{
    internal class CipherEncoder : MessageEncoder
    {
        public override string EncodingName
        {
            get { return "cipher"; }
        }

        public override void Decode(IEncodedMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data))
                return;

            var currentEncoding = GetCurrentEncoding(payload);
            if (currentEncoding.Contains(EncodingName) == false)
                return;

            if (options.Encrypted == false)
            {
                throw new AblyException("Message cannot be decrypted as the channel is not set up for encryption & decryption", 92001);
            }

            var cipherType = GetCipherType(currentEncoding);
            if (cipherType.ToLower() != options.CipherParams.CipherType.ToLower())
            {
                throw new AblyException(string.Format("Cipher algorithm {0} does not match message cipher algorithm of {1}", options.CipherParams.CipherType.ToLower(), currentEncoding), 92002);
            }

            var cipher = Crypto.GetCipher(options);
            payload.data = cipher.Decrypt(payload.data as byte[]);
            RemoveCurrentEncodingPart(payload);
        }

        private string GetCipherType(string currentEncoding)
        {
            var parts = currentEncoding.Split('+');
            if (parts.Length == 2)
                return parts[1];
            return "";
        }

        public override void Encode(IEncodedMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data) || IsEncrypted(payload))
                return;

            if (options.Encrypted == false)
                return;

            if (payload.data is string)
            {
                payload.data = ((string)payload.data).GetBytes();
                AddEncoding(payload, "utf-8");
            }

            var cipher = Crypto.GetCipher(options);
            payload.data = cipher.Encrypt(payload.data as byte[]);
            AddEncoding(payload, string.Format("{0}+{1}", EncodingName, options.CipherParams.CipherType.ToLower()));
        }

        private bool IsEncrypted(IEncodedMessage payload)
        {
            return payload.encoding.IsNotEmpty() && payload.encoding.Contains(EncodingName);
        }

        public CipherEncoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}