using FluentAssertions;
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

                var context = new EncodingDecodingContext();
                MessageHandler.DecodePayload(payload, context);

                ((byte[])context.BaseEncodedPreviousPayload).Should().BeEquivalentTo(_binaryData);
            }

            [Fact]
            public void WhenBase64IsNotTheFirstEncoding_ShouldSaveTheOriginalPayloadInContext()
            {
                var message = new Message() { Data = new { Text = "Hello" } };
                MessageHandler.EncodePayload(message, new EncodingDecodingContext());
                var payloadData = message.Data as string;

                var context = new EncodingDecodingContext();
                MessageHandler.DecodePayload(message, context);
                context.BaseEncodedPreviousPayload.Should().Be(payloadData);
            }
        }

        public MessageDecodingAcceptanceTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
