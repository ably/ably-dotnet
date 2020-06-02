using System.Security.Cryptography;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.MessageEncoders;
using IO.Ably.Tests;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.AcceptanceTests
{
    public class MessageDecodingAcceptanceTests : AblySpecs
    {
        public class WithBase64Message
        {
            private string _stringData;
            private byte[] _binaryData;
            private string _base64Data;

            public WithBase64Message()
            {
                _stringData = "random-string";
                _binaryData = _stringData.GetBytes();
                _base64Data = _binaryData.ToBase64();
            }

            [Fact]
            public void WithBase64EncodingBeforeOtherEncodings_SavesDecodedDataToTheContext()
            {
                var payload = new Message() { Data = _base64Data, Encoding = "utf-8/base64" };

                var context = new DecodingContext();
                MessageHandler.DecodePayload(payload, context);

                context.PreviousPayload.Should().BeEquivalentTo(_binaryData);
                context.PreviousPayloadEncoding.Should().BeEquivalentTo("utf-8");
            }

            [Fact]
            public void WhenBase64IsNotTheFirstEncoding_ShouldSaveTheOriginalPayloadInContext()
            {
                var message = new Message() { Data = new { Text = "Hello" } };
                MessageHandler.EncodePayload(message, new DecodingContext());
                var payloadData = message.Data as string;
                var payloadEncoding = message.Encoding;

                var context = new DecodingContext();
                MessageHandler.DecodePayload(message, context);
                context.PreviousPayload.Should().BeEquivalentTo(payloadData.GetBytes());
                context.PreviousPayloadEncoding.Should().Be(payloadEncoding);
            }

            [Fact]
            public void WithFailedEncoding_ShouldLeaveOriginalDataAndEncodingInPayload()
            {
                var initialEncoding = "utf-8/cipher+aes-128-cbc";
                var encryptedValue = "test";
                var payload = new Message() { Data = encryptedValue, Encoding = initialEncoding };

                var channelOptions =
                    new ChannelOptions(true, new CipherParams(
                        Crypto.DefaultAlgorithm,
                        GenerateKey(Crypto.DefaultKeylength),
                        Encryption.CipherMode.CBC));

                var context = channelOptions.ToEncodingDecodingContext();
                var result = MessageHandler.DecodePayload(payload, context);

                result.IsFailure.Should().BeTrue();
                payload.Encoding.Should().Be(initialEncoding);
                payload.Data.Should().Be(encryptedValue);
            }

            private byte[] GenerateKey(int keyLength)
            {
                var keyGen = new Rfc2898DeriveBytes("password", 8);
                return keyGen.GetBytes(keyLength / 8);
            }
        }

        public MessageDecodingAcceptanceTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
