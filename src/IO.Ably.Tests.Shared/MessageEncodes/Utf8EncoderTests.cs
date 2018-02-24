using FluentAssertions;
using IO.Ably.MessageEncoders;
using Xunit;

namespace IO.Ably.Tests.MessageEncodes
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
            encoder = new Utf8Encoder(Defaults.Protocol);
        }

        private Message DecodePayload(object data, string encoding = "")
        {
            var payload = new Message() { Data = data, Encoding = encoding };
            encoder.Decode(payload, new ChannelOptions());
            return payload;
        }

        public class Decode : Utf8EncoderTests
        {
            [Fact]
            public void WithUtf8Payload_ConvertsDataAndStripsEncoding()
            {
                var payload = DecodePayload(_byteData, "utf-8");

                payload.Data.Should().Be(_stringData);
                payload.Encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithUtf8PayloadBeforeOthers_StringEncodingCorrectly()
            {
                var payload = DecodePayload(_byteData, "json/utf-8");

                payload.Data.Should().Be(_stringData);
                payload.Encoding.Should().Be("json");
            }

            [Fact]
            public void WithAnotherPayload_LeavesDataAndEncoding()
            {
                var payload = DecodePayload("test", "json");

                payload.Data.Should().Be("test");
                payload.Encoding.Should().Be("json");
            }
        }
    }
}