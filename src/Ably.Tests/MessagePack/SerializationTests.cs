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
            
            ((MessagePackObject)resultMessage.Data).ToObject().Should().Be(message.Data);
            resultMessage.Name.Should().Be(message.Name);

        }
            
        [Fact]
        public void CanSerialiseAndDeserialiseBase64ByteArray()
        {
            var message = new Message() {Name = "example", Data = "AAECAwQFBgcICQoLDA0ODw==".FromBase64()};
            var serialised = MsgPackHelper.Serialise(new List<Message> { message });
            Console.WriteLine(serialised.ToBase64());
            var resultMessage = MsgPackHelper.DeSerialise(serialised, typeof(List<Message>)) as List<Message>;
            var data = ((MessagePackObject)resultMessage.First().Data).ToObject() as byte[];
            data.Should().BeEquivalentTo(message.Data as byte[]);
            resultMessage.First().Name.Should().Be(message.Name);
        }

        [Fact]
        public void CanDeserialiseTokenResponse()
        {
            var value =
                "gaxhY2Nlc3NfdG9rZW6GpXRva2Vu2gCIZzRYNlFRLkR5QmM5TGVHb3ctbGllRHBuM010bHdPblBIaDdlbTdzMkNyU2daSzNjVDZEb2VKNXZUMVl0cDQxb2k1VlFLTVJMbklXQ3JBWnR1Tm9xeUNJb1RaYUIxX29RRV9FLW93NmN4Sl9RMHBVMmd5aW9sUTRqdVQzNU4yNEM4M3dKemlCOaNrZXmtZzRYNlFRLnV0ekdsZ6lpc3N1ZWRfYXTOVMEP1qdleHBpcmVzzlTBHeaqY2FwYWJpbGl0eYGhKpGhKqhjbGllbnRJZKMxMjM=";

            var decodedMessagePack = MsgPackHelper.DeSerialise(value.FromBase64(), typeof (MessagePackObject)).ToString();

            var response = JsonConvert.DeserializeObject<TokenResponse>(decodedMessagePack);

            response.AccessToken.Should().NotBeNull();
            response.AccessToken.KeyName.Should().Be("g4X6QQ.utzGlg");
            response.AccessToken.Capability.ToJson().Should().Be("{ \"*\": [ \"*\" ] }");
            response.AccessToken.ClientId.Should().Be("123");
            response.AccessToken.Token.Should().Be("g4X6QQ.DyBc9LeGow-lieDpn3MtlwOnPHh7em7s2CrSgZK3cT6DoeJ5vT1Ytp41oi5VQKMRLnIWCrAZtuNoqyCIoTZaB1_oQE_E-ow6cxJ_Q0pU2gyiolQ4juT35N24C83wJziB9");
            response.AccessToken.IssuedAt.Should().Be(((long)1421938646).FromUnixTime());
            response.AccessToken.Expires.Should().Be(((long)1421942246).FromUnixTime());
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
		""issued"": 1421937735,
		""expires"": 1421941335,
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
            response.AccessToken.IssuedAt.Should().Be(((long)1421937735).FromUnixTime());
            response.AccessToken.Expires.Should().Be(((long)1421941335).FromUnixTime());
        }
    }
}
