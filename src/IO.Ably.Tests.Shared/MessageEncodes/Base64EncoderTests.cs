using FluentAssertions;
using IO.Ably.MessageEncoders;
using Xunit;

namespace IO.Ably.Tests.MessageEncodes
{
    public class Base64EncoderTests
    {
        private string _stringData;
        private byte[] _binaryData;
        private string _base64Data;
        private Base64Encoder _encoder;

        public Base64EncoderTests(Protocol? protocol = null)
        {
            _stringData = "random-string";
            _binaryData = _stringData.GetBytes();
            _base64Data = _binaryData.ToBase64();
            _encoder = new Base64Encoder(protocol ?? Defaults.Protocol);
        }

        public class Decode : Base64EncoderTests
        {
            [Fact]
            public void WithBase64EncodedPayload_ConvertsItBackToBinaryData()
            {
                var payload = new Message() { Data = _base64Data, Encoding = "base64" };

                _encoder.Decode(payload, new ChannelOptions());

                ((byte[])payload.Data).Should().BeEquivalentTo(_binaryData);
                payload.Encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithBase64EncodingBeforeOtherEncodings_ConvertsDataAndStripsEncodingCorrectly()
            {
                var payload = new Message() { Data = _base64Data, Encoding = "utf-8/base64" };

                _encoder.Decode(payload, new ChannelOptions());

                ((byte[])payload.Data).Should().BeEquivalentTo(_binaryData);
                payload.Encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithMessageAnotherEncoding_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() { Data = _stringData, Encoding = "utf-8" };

                _encoder.Decode(payload, new ChannelOptions());

                payload.Data.Should().Be(_stringData);
                payload.Encoding.Should().Be("utf-8");
            }
        }

        public class EncodeWithBinaryProtocol : Base64EncoderTests
        {
            public EncodeWithBinaryProtocol()
                : base(Defaults.Protocol)
            {
            }

            [Fact]
            public void WithBinaryData_LeavesDataAndEncodingIntact()
            {
                if (!Config.MsgPackEnabled)
                {
                    return;
                }

                var payload = new Message() { Data = _binaryData };

                _encoder.Encode(payload, new ChannelOptions());

                payload.Data.Should().Be(_binaryData);
                payload.Encoding.Should().BeNull();
            }
        }

        public class EncodeWithTextProtocol : Base64EncoderTests
        {
            public EncodeWithTextProtocol()
                : base(Protocol.Json)
            {
            }

            [Fact]
            public void WithBinaryPayloadWithoutPriorEncoding_ConvertsDataToBase64StringAndSetsEnconding()
            {
                var payload = new Message() { Data = _binaryData };

                _encoder.Encode(payload, new ChannelOptions());

                payload.Data.Should().Be(_base64Data);
                payload.Encoding.Should().Be(_encoder.EncodingName);
            }

            [Fact]
            public void WithBinaryPayloadAndExsitingEncoding_ConvertsDataToBase64StringAndAddsBase64Encoding()
            {
                var payload = new Message() { Data = _binaryData, Encoding = "cipher" };

                _encoder.Encode(payload, new ChannelOptions());

                payload.Data.Should().Be(_base64Data);
                payload.Encoding.Should().Be("cipher/" + _encoder.EncodingName);
            }

            [Fact]
            public void WithStringPayload_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() { Data = _stringData };

                _encoder.Encode(payload, new ChannelOptions());

                payload.Data.Should().Be(_stringData);
                payload.Encoding.Should().BeNull();
            }

            [Fact]
            public void WithEmptyPayload_LeavesDataAndEncodingIntact()
            {
                var payload = new Message();

                _encoder.Encode(payload, new ChannelOptions());

                payload.Data.Should().BeNull();
                payload.Encoding.Should().BeNull();
            }
        }
    }
}
