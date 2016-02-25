using FluentAssertions;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace IO.Ably.Tests.MessageEncodes
{
    public class JsonEncoderTests
    {
        private object _objectData;
        private string _jsonData;
        private int[] _arrayData = new []{ 1, 2, 3};
        private string _jsonArrayData = "[1,2,3]";
        private JsonEncoder encoder;

        public JsonEncoderTests()
        {
            _objectData = new { Test = "test", Best = "best"};
            _jsonData = JsonConvert.SerializeObject(_objectData);
            encoder = new JsonEncoder(Protocol.MsgPack);
        }

        private Message EncodePayload(object data, string encoding = "")
        {
            var payload = new Message() {data = data, encoding = encoding};
            encoder.Encode(payload, new ChannelOptions());
            return payload;
        }

        private Message DecodePayload(object data, string encoding = "")
        {
            var payload = new Message() { data = data, encoding = encoding };
            encoder.Decode(payload, new ChannelOptions());
            return payload;
        }

        public class Decode : JsonEncoderTests
        {
            [Fact]
            public void WithJsonPayload_ConvertsDataToJObjectAndStripsEncoding()
            {
                var payload = DecodePayload(_jsonData, "json");

                payload.data.Should().BeOfType<JObject>();

                var obj =(payload.data as JObject).ToObject(_objectData.GetType());
                obj.Should().Be(_objectData);

                payload.encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithJsonPayloadBeforeOtherPayload_ConvertsDataToJObjecAndStrinpsEncoding()
            {
                var payload = DecodePayload(_jsonData, "utf-8/json");

                payload.data.Should().BeOfType<JObject>();
                payload.encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithAnotherPayload_LeavesDataAndEncoding()
            {
                var payload = DecodePayload("test", "utf-8");

                payload.data.Should().Be("test");
                payload.encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithInvalidJsonPayload_ThrowsAblyException()
            {
                var error = Assert.Throws<AblyException>(delegate { DecodePayload("test", "json"); });
                error.ErrorInfo.message.Should().Be("Invalid Json data: 'test'");
            }


        }

        public class Encode : JsonEncoderTests
        {
            [Fact]
            public void WithObject_ConvertDataToJsonStringAndSetsCorrectEncoding()
            {
                var payload = EncodePayload(_objectData);

                payload.data.Should().Be(_jsonData);
                payload.encoding.Should().Be("json");
            }

            [Fact]
            public void WithObjectWithExistingEncoding_ConvertsDataAndAppendsEncoding()
            {
                var payload = EncodePayload(_objectData, "utf-8");

                payload.data.Should().Be(_jsonData);
                payload.encoding.Should().Be("utf-8/json");
            }

            [Fact]
            public void WithArray_ConvertsDataCorrectly()
            {
                var payload = EncodePayload(_arrayData);

                payload.data.Should().Be(_jsonArrayData);
                payload.encoding.Should().Be("json");
            }

            [Fact]
            public void WithStringData_LeavesEncodingAndDataIntact()
            {
                var payload = EncodePayload("test");

                payload.data.Should().Be("test");
                payload.encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithNullData_LeavesEncodingAndDataIntact()
            {
                var payload = EncodePayload(null);

                payload.data.Should().BeNull();
                payload.encoding.Should().BeEmpty();
            }
        }

    }
}