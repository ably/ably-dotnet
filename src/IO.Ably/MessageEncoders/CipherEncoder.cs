using System;
using IO.Ably.Encryption;
using IO.Ably.Platform;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class CipherEncoder : MessageEncoder
    {
        public override string EncodingName => "cipher";

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data))
                return Result.Ok();

            var currentEncoding = GetCurrentEncoding(payload);
            if (currentEncoding.Contains(EncodingName) == false)
                return Result.Ok();

            var cipherType = GetCipherType(currentEncoding);
            if (cipherType.ToLower() != options.CipherParams.CipherType.ToLower())
            {
                Logger.Error(
                    $"Cipher algorithm {options.CipherParams.CipherType.ToLower()} does not match message cipher algorithm of {currentEncoding}");
                return Result.Fail(new ErrorInfo($"Cipher algorithm {options.CipherParams.CipherType.ToLower()} does not match message cipher algorithm of {currentEncoding}"));
            }

            var cipher = Crypto.GetCipher(options.CipherParams);
            try
            {
                payload.data = cipher.Decrypt(payload.data as byte[]);
                RemoveCurrentEncodingPart(payload);
                return Result.Ok();
            }
            catch (AblyException ex)
            {
                Logger.Error($"Error decrypting payload using cypher {options.CipherParams.CipherType}. Leaving it encrypted", ex);
                return Result.Fail($"Error decrypting payload using cypher {options.CipherParams.CipherType}. Leaving it encrypted");
            }
        }

        private string GetCipherType(string currentEncoding)
        {
            var parts = currentEncoding.Split('+');
            if (parts.Length == 2)
                return parts[1];
            return "";
        }

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data) || IsEncrypted(payload))
                return Result.Ok();

            if (options.Encrypted == false)
                return Result.Ok();

            if (payload.data is string)
            {
                payload.data = ((string)payload.data).GetBytes();
                AddEncoding(payload, "utf-8");
            }

            var cipher = Crypto.GetCipher(options.CipherParams);
            payload.data = cipher.Encrypt(payload.data as byte[]);
            AddEncoding(payload, $"{EncodingName}+{options.CipherParams.CipherType.ToLower()}");

            return Result.Ok();
        }

        private bool IsEncrypted(IMessage payload)
        {
            return payload.encoding.IsNotEmpty() && payload.encoding.Contains(EncodingName);
        }

        public CipherEncoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}