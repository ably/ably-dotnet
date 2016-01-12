using System;
using System.Collections.Generic;
using System.Linq;
using Ably.CustomSerialisers;
using FluentAssertions;
using MsgPack;
using Newtonsoft.Json;
using Xunit;

namespace Ably.Tests.MessagePack
{
    public class MessagePackSerializationTests
    {
        public MessagePackSerializationTests()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>() {  new DateTimeOffsetJsonConverter(),
                    new CapabilityJsonConverter() }
            };
        }

        [Fact]
        public void CanSerialiseListOfMessagesAndDeserialiseThem()
        {
            var message = new Message("example", "The quick brown fox jumped over the lazy dog");
            var serialised = MsgPackHelper.Serialise(new List<Message> {message});

            var result = MsgPackHelper.DeSerialise(serialised, typeof (List<Message>)) as List<Message>;
            var resultMessage = result.First();

            ((MessagePackObject)resultMessage.data).ToObject().Should().Be(message.data);
            resultMessage.name.Should().Be(message.name);

        }

        [Fact]
        public void CanSerialiseAndDeserialiseBase64ByteArray()
        {
            var message = new Message() {name = "example", data = "AAECAwQFBgcICQoLDA0ODw==".FromBase64()};
            var serialised = MsgPackHelper.Serialise(new List<Message> { message });
            Console.WriteLine(serialised.ToBase64());
            var resultMessage = MsgPackHelper.DeSerialise(serialised, typeof(List<Message>)) as List<Message>;
            var data = ((MessagePackObject)resultMessage.First().data).ToObject() as byte[];
            data.Should().BeEquivalentTo(message.data as byte[]);
            resultMessage.First().name.Should().Be(message.name);
        }

        [Fact]
        public void CanDeserialiseTokenResponse()
        {
            var value =
                "gaxhY2Nlc3NfdG9rZW6GpXRva2Vu2YhnNFg2UVEuRHlCYzlMZUdvdy1saWVEcG4zTXRsd09uUEhoN2VtN3MyQ3JTZ1pLM2NUNkRvZUo1dlQxWXRwNDFvaTVWUUtNUkxuSVdDckFadHVOb3F5Q0lvVFphQjFfb1FFX0Utb3c2Y3hKX1EwcFUyZ3lpb2xRNGp1VDM1TjI0Qzgzd0p6aUI5p2tleU5hbWWtZzRYNlFRLnV0ekdsZ6Zpc3N1ZWTOVMEP1qdleHBpcmVzzlTBHeaqY2FwYWJpbGl0eYGhKpGhKqhjbGllbnRJZKMxMjM=";

            var decodedMessagePack = MsgPackHelper.DeSerialise(value.FromBase64(), typeof (MessagePackObject)).ToString();

            Console.WriteLine(DateTimeOffset.Now.ToUnixTimeInMilliseconds());

            var response = JsonConvert.DeserializeObject<TokenResponse>(decodedMessagePack);

            response.AccessToken.Should().NotBeNull();
            response.AccessToken.KeyName.Should().Be("g4X6QQ.utzGlg");
            response.AccessToken.Capability.ToJson().Should().Be("{ \"*\": [ \"*\" ] }");
            response.AccessToken.ClientId.Should().Be("123");
            response.AccessToken.Token.Should().Be("g4X6QQ.DyBc9LeGow-lieDpn3MtlwOnPHh7em7s2CrSgZK3cT6DoeJ5vT1Ytp41oi5VQKMRLnIWCrAZtuNoqyCIoTZaB1_oQE_E-ow6cxJ_Q0pU2gyiolQ4juT35N24C83wJziB9");
            response.AccessToken.Issued.Should().Be(((long)1421938646).FromUnixTimeInMilliseconds());
            response.AccessToken.Expires.Should().Be(((long)1421942246).FromUnixTimeInMilliseconds());
        }
    }

    public class JsonSerializationTests
    {
        public JsonSerializationTests()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>() {  new DateTimeOffsetJsonConverter(),
                    new CapabilityJsonConverter() }
            };
        }

        [Fact]
        public void CanDeserialiseTokenResponse()
        {
            var value = @"{
	""access_token"": {
		""token"": ""_SYo4Q.D3WmHhU"",
		""keyName"": ""_SYo4Q.j8mhAQ"",
		""issued"": 1449163326485,
		""expires"": 1449163326485,
		""capability"": {
			""*"": [
				""*""
			]
		},
		""clientId"": ""123""
	}
}";



            var response = JsonConvert.DeserializeObject<TokenResponse>(value);

            response.AccessToken.Should().NotBeNull();
            response.AccessToken.KeyName.Should().Be("_SYo4Q.j8mhAQ");
            response.AccessToken.Capability.ToJson().Should().Be("{ \"*\": [ \"*\" ] }");
            response.AccessToken.ClientId.Should().Be("123");
            response.AccessToken.Token.Should().Be("_SYo4Q.D3WmHhU");
            response.AccessToken.Issued.Should().Be(((long)1449163326485).FromUnixTimeInMilliseconds());
            response.AccessToken.Expires.Should().Be(((long)1449163326485).FromUnixTimeInMilliseconds());
        }
    }
}
