using System.Security.Cryptography;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.MessageEncoders;
using IO.Ably.Tests;
using IO.Ably.Tests.Shared.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.AcceptanceTests
{
    public class MessageDecodingAcceptanceTests : AblySpecs
    {
        public class WithBase64Message
        {
            private readonly byte[] _binaryData;
            private readonly string _base64Data;

            public WithBase64Message()
            {
                const string stringData = "random-string";

                _binaryData = stringData.GetBytes();
                _base64Data = _binaryData.ToBase64();
            }

            [Fact]
            public void WithBase64EncodingBeforeOtherEncodings_SavesDecodedDataToTheContext()
            {
                var payload = new Message { Data = _base64Data, Encoding = "utf-8/base64" };

                var logger = DefaultLogger.LoggerInstance;
                var context = new DecodingContext(logger);
                MessageHandler.DecodePayload(payload, context, logger);

                context.PreviousPayload.GetBytes().Should().BeEquivalentTo(_binaryData);
                context.PreviousPayload.Encoding.Should().BeEquivalentTo("utf-8");
            }

            [Fact]
            public void WhenBase64IsNotTheFirstEncoding_ShouldSaveTheOriginalPayloadInContext()
            {
                var message = new Message { Data = new { Text = "Hello" } };

                var logger = DefaultLogger.LoggerInstance;
                MessageHandler.EncodePayload(message, new DecodingContext(logger));
                var payloadData = message.Data as string;
                var payloadEncoding = message.Encoding;

                var context = new DecodingContext(logger);
                MessageHandler.DecodePayload(message, context, logger);
                context.PreviousPayload.GetBytes().Should().BeEquivalentTo(payloadData.GetBytes());
                context.PreviousPayload.Encoding.Should().Be(payloadEncoding);
            }

            [Fact]
            public void WithFailedEncoding_ShouldLeaveOriginalDataAndEncodingInPayload()
            {
                const string initialEncoding = "utf-8/cipher+aes-128-cbc";
                const string encryptedValue = "test";
                var payload = new Message { Data = encryptedValue, Encoding = initialEncoding };

                var channelOptions =
                    new ChannelOptions(true, new CipherParams(
                        Crypto.DefaultAlgorithm,
                        GenerateKey(Crypto.DefaultKeylength),
                        Encryption.CipherMode.CBC));

                var logger = DefaultLogger.LoggerInstance;

                var context = channelOptions.ToDecodingContext(logger);
                var result = MessageHandler.DecodePayload(payload, context, logger);

                result.IsFailure.Should().BeTrue();
                payload.Encoding.Should().Be(initialEncoding);
                payload.Data.Should().Be(encryptedValue);
            }

            private byte[] GenerateKey(int keyLength)
            {
#if NET6_0_OR_GREATER
                var keyGen = new Rfc2898DeriveBytes("password", 8, 8, HashAlgorithmName.SHA256);
#else
                var keyGen = new Rfc2898DeriveBytes("password", 8);
#endif
                return keyGen.GetBytes(keyLength / 8);
            }
        }

        public MessageDecodingAcceptanceTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
