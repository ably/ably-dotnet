using IO.Ably;
using IO.Ably.Encryption;

namespace IO.Ably.MessageEncoders
{
    internal class CipherEncoder : MessageEncoder
    {
        public override string EncodingName => "cipher";

        public override Result Decode(IMessage payload, EncodingDecodingContext context)
        {
            var options = context.ChannelOptions;
            Logger = options?.Logger ?? DefaultLogger.LoggerInstance;

            if (IsEmpty(payload.Data))
            {
                return Result.Ok();
            }

            var currentEncoding = GetCurrentEncoding(payload);
            if (currentEncoding.Contains(EncodingName) == false)
            {
                return Result.Ok();
            }

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
                if ((payload.Data is byte[]) == false)
                {
                    return Result.Fail("Expected data to be byte[] but received " + payload.Data.GetType());
                }

                payload.Data = cipher.Decrypt(payload.Data as byte[]);
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
            {
                return parts[1];
            }

            return string.Empty;
        }

        public override Result Encode(IMessage payload, EncodingDecodingContext context)
        {
            var options = context.ChannelOptions;
            if (IsEmpty(payload.Data) || IsEncrypted(payload))
            {
                return Result.Ok();
            }

            if (options.Encrypted == false)
            {
                return Result.Ok();
            }

            if (payload.Data is string data)
            {
                payload.Data = data.GetBytes();
                AddEncoding(payload, "utf-8");
            }

            var cipher = Crypto.GetCipher(options.CipherParams);
            payload.Data = cipher.Encrypt(payload.Data as byte[]);
            AddEncoding(payload, $"{EncodingName}+{options.CipherParams.CipherType.ToLower()}");

            return Result.Ok();
        }

        private bool IsEncrypted(IMessage payload)
        {
            return payload.Encoding.IsNotEmpty() && payload.Encoding.Contains(EncodingName);
        }

        public CipherEncoder()
            : base()
        {
        }
    }
}
