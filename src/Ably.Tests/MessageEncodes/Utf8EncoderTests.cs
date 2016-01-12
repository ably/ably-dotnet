using Ably.MessageEncoders;
using Ably.Rest;
using FluentAssertions;
using Xunit;

namespace Ably.Tests.MessageEncodes
{
    public class Utf8EncoderTests
    {
        private string _stringData;
        private byte[] _byteData;
        private Utf8Encoder encoder;

        public Utf8EncoderTests()
        {
            _stringData = "random_string";
            _byteData = _stringData.GetBytes();
            encoder = new Utf8Encoder(Protocol.MsgPack);
        }

        private Message DecodePayload(object data, string encoding = "")
        {
            var payload = new Message() { data = data, encoding = encoding };
            encoder.Decode(payload, new ChannelOptions());
            return payload;
        }

        public class Decode : Utf8EncoderTests
        {
            [Fact]
            public void WithUtf8Payload_ConvertsDataAndStripsEncoding()
            {
                var payload = DecodePayload(_byteData, "utf-8");

                payload.data.Should().Be(_stringData);
                payload.encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithUtf8PayloadBeforeOthers_StringEncodingCorrectly()
            {
                var payload = DecodePayload(_byteData, "json/utf-8");

                payload.data.Should().Be(_stringData);
                payload.encoding.Should().Be("json");
            }

            [Fact]
            public void WithAnotherPayload_LeavesDataAndEncoding()
            {
                var payload = DecodePayload("test", "json");

                payload.data.Should().Be("test");
                payload.encoding.Should().Be("json");
            }
        }
    }
}