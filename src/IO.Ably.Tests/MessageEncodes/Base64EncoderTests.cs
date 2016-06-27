using IO.Ably;
using FluentAssertions;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using Xunit;

namespace IO.Ably.Tests.MessageEncodes
{
    public class Base64EncoderTests
    {
        private string _stringData;
        private byte[] _binaryData;
        private string _base64Data;
            private Base64Encoder encoder;

        public Base64EncoderTests(Protocol? protocol = null)
        {
            _stringData = "random-string";
            _binaryData = _stringData.GetBytes();
            _base64Data = _binaryData.ToBase64();
            encoder = new Base64Encoder(protocol ?? Protocol.MsgPack);
        }


        public class Decode :Base64EncoderTests
        {
            [Fact]
            public void WithBase64EncodedPayload_ConvertsItBackToBinaryData()
            {
                var payload = new Message() {data = _base64Data, encoding = "base64"};

                encoder.Decode(payload, new ChannelOptions());

                ((byte[])payload.data).Should().BeEquivalentTo(_binaryData);
                payload.encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithBase64EncodingBeforeOtherEncodings_ConvertsDataAndStripsEncodingCorrectly()
            {
                var payload = new Message() { data = _base64Data, encoding = "utf-8/base64" };

                encoder.Decode(payload, new ChannelOptions());

                ((byte[])payload.data).Should().BeEquivalentTo(_binaryData);
                payload.encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithMessageAnotherEncoding_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() {data = _stringData, encoding = "utf-8"};

                encoder.Decode(payload, new ChannelOptions());

                payload.data.Should().Be(_stringData);
                payload.encoding.Should().Be("utf-8");
            }
        }


        public class EncodeWithBinaryProtocol : Base64EncoderTests
        {
            public EncodeWithBinaryProtocol() : base(Protocol.MsgPack)
            {

            }

            [Fact]
            public void WithBinaryData_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() {data = _binaryData};

                encoder.Encode(payload, new ChannelOptions());

                payload.data.Should().Be(_binaryData);
                payload.encoding.Should().BeNull();
            }
        }

        public class EncodeWithTextProtocol : Base64EncoderTests
        {
            public EncodeWithTextProtocol() :base(Protocol.Json)
            {
            }

            [Fact]
            public void WithBinaryPayloadWithoutPriorEncoding_ConvertsDataToBase64StringAndSetsEnconding()
            {
                var payload = new Message() {data = _binaryData};

                encoder.Encode(payload, new ChannelOptions());

                payload.data.Should().Be(_base64Data);
                payload.encoding.Should().Be(encoder.EncodingName);
            }

            [Fact]
            public void WithBinaryPayloadAndExsitingEncoding_ConvertsDataToBase64StringAndAddsBase64Encoding()
            {
                var payload = new Message() {data = _binaryData, encoding = "cipher"};

                encoder.Encode(payload, new ChannelOptions());

                payload.data.Should().Be(_base64Data);
                payload.encoding.Should().Be("cipher/" + encoder.EncodingName);
            }

            [Fact]
            public void WithStringPayload_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() {data = _stringData};

                encoder.Encode(payload, new ChannelOptions());

                payload.data.Should().Be(_stringData);
                payload.encoding.Should().BeNull();
            }

            [Fact]
            public void WithEmptyPayload_LeavesDataAndEncodingIntact()
            {
                var payload = new Message();

                encoder.Encode(payload, new ChannelOptions());

                payload.data.Should().BeNull();
                payload.encoding.Should().BeNull();
            }
        }
    }
}
