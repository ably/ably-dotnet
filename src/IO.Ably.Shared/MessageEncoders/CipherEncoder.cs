using IO.Ably;
using IO.Ably.Encryption;

namespace IO.Ably.MessageEncoders
{
    internal class CipherEncoder : MessageEncoder
    {
        internal ILogger Logger { get; set; }
        public override string EncodingName => "cipher";

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            Logger = options.Logger ?? IO.Ably.DefaultLogger.LoggerInstance;

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

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.Data) || IsEncrypted(payload))
            {
                return Result.Ok();
            }

            if (options.Encrypted == false)
            {
                return Result.Ok();
            }

            if (payload.Data is string)
            {
                payload.Data = ((string)payload.Data).GetBytes();
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

        public CipherEncoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}