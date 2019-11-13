using System;
using IO.Ably;
using IO.Ably.Encryption;

namespace IO.Ably.MessageEncoders
{
    internal class CipherEncoder : MessageEncoder
    {
        public const string EncodingNameStr = "cipher";

        public override string EncodingName => EncodingNameStr;

        public override Result<ProcessedPayload> Decode(IPayload payload, EncodingDecodingContext context)
        {
            var options = context.ChannelOptions;
            Logger = options?.Logger ?? DefaultLogger.LoggerInstance;

            if (IsEmpty(payload.Data))
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            var currentEncoding = GetCurrentEncoding(payload);
            if (currentEncoding.Contains(EncodingName) == false)
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            var cipherType = GetCipherType(currentEncoding);
            if (cipherType.ToLower() != options.CipherParams.CipherType.ToLower())
            {
                Logger.Error(
                    $"Cipher algorithm {options.CipherParams.CipherType.ToLower()} does not match message cipher algorithm of {currentEncoding}");
                return Result.Fail<ProcessedPayload>(new ErrorInfo($"Cipher algorithm {options.CipherParams.CipherType.ToLower()} does not match message cipher algorithm of {currentEncoding}"));
            }

            var cipher = Crypto.GetCipher(options.CipherParams);
            try
            {
                if ((payload.Data is byte[]) == false)
                {
                    return Result.Fail<ProcessedPayload>(new ErrorInfo("Expected data to be byte[] but received " + payload.Data.GetType()));
                }

                return Result.Ok(new ProcessedPayload()
                {
                    Data = payload.Data = cipher.Decrypt(payload.Data as byte[]),
                    Encoding = RemoveCurrentEncodingPart(payload),
                });
            }
            catch (AblyException ex)
            {
                Logger.Error($"Error decrypting payload using cypher {options.CipherParams.CipherType}. Leaving it encrypted", ex);
                return Result.Fail<ProcessedPayload>(new ErrorInfo($"Error decrypting payload using cypher {options.CipherParams.CipherType}. Leaving it encrypted"));
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

        public override bool CanProcess(string currentEncoding)
        {
            return currentEncoding.IsNotEmpty() && currentEncoding.StartsWith(EncodingNameStr, StringComparison.InvariantCultureIgnoreCase);
        }

        public override Result<ProcessedPayload> Encode(IPayload payload, EncodingDecodingContext context)
        {
            var options = context.ChannelOptions;
            if (IsEmpty(payload.Data) || IsEncrypted(payload))
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            if (options.Encrypted == false)
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            if (payload.Data is string data)
            {
                return Result.Ok(new ProcessedPayload()
                {
                    Data = data.GetBytes(),
                    Encoding = AddEncoding(payload, "utf-8"),
                });
            }

            var cipher = Crypto.GetCipher(options.CipherParams);
            var result = new ProcessedPayload()
            {
                Data = cipher.Encrypt(payload.Data as byte[]),
                Encoding = AddEncoding(payload, $"{EncodingName}+{options.CipherParams.CipherType.ToLower()}"),
            };

            return Result.Ok(result);
        }

        private bool IsEncrypted(IPayload payload)
        {
            return payload.Encoding.IsNotEmpty() && payload.Encoding.Contains(EncodingName);
        }

        public CipherEncoder()
            : base()
        {
        }
    }
}
