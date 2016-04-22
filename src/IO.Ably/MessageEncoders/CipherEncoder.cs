using System;
using IO.Ably.Encryption;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
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

            var cipherType = GetCipherType(currentEncoding);
            if (cipherType.ToLower() != options.CipherParams.CipherType.ToLower())
            {
                throw new AblyException(string.Format("Cipher algorithm {0} does not match message cipher algorithm of {1}", options.CipherParams.CipherType.ToLower(), currentEncoding), 92002);
            }

            var cipher = Crypto.GetCipher(options);
            try
            {
                payload.data = cipher.Decrypt(payload.data as byte[]);
                RemoveCurrentEncodingPart(payload);
            }
            catch (AblyException ex)
            {
                Logger.Error("Error decrypting payload. Leaving it encrypted", ex); 
            }
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
            return StringExtensions.IsNotEmpty(payload.encoding) && payload.encoding.Contains(EncodingName);
        }

        public CipherEncoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}