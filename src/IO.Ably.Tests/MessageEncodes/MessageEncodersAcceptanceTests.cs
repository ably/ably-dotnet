using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.Platform;
using IO.Ably.Rest;
using IO.Ably.Tests;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.AcceptanceTests
{
    public class MessageEncodersAcceptanceTests : AblySpecs
    {
        public class WithSupportedPayloads : MockHttpSpecs
        {
            public WithSupportedPayloads(ITestOutputHelper output) : base(output)
            {
            }

            public class TestObject
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public DateTime DoB { get; set; }
            }

            public static IEnumerable<object[]> SupportedMessages
            {  
                get
                {
                    
                    yield return new object[] {new Message("string", "string"), null};
                    yield return new object[] {new Message("string", new byte[] {1,2,4}), "base64"};
                    yield return new object[] { new Message("object", new TestObject() { Age = 40, Name = "Bob", DoB = new DateTime(1976, 1,1)}), "json"};
                }
            }

            public static IEnumerable<object[]> UnSupportedMassages
            {
                get
                {
                    yield return new object[] { new Message("int", 1) };
                    yield return new object[] { new Message("float", 1.1) };
                    yield return new object[] { new Message("date", Config.Now())};
                }
            }

            [Theory]
            [MemberData(nameof(UnSupportedMassages))]
            [Trait("spec", "RSL4a")]
            public async Task PublishUnSupportedMessagesThrows(Message message)
            {
                var client = GetRestClient();

                var ex = await Assert.ThrowsAsync<AblyException>(() => client.Channels.Get("test").Publish(message));
                ex.Message.Should().Contain("Unsupported payload");
            }

            [Theory]
            [MemberData("SupportedMessages")]
            [Trait("spec", "RSL4a")]
            public async Task PublishSupportedMessages(Message message, string encoding)
            {
                var client = GetRestClient();

                await client.Channels.Get("test").Publish(message);

                var processedMessages = JsonConvert.DeserializeObject<List<Message>>(LastRequest.RequestBody.GetText());
                processedMessages.First().encoding.Should().Be(encoding);
                Output.WriteLine("Encoded message: " + LastRequest.RequestBody.GetText());
            }
            
        }


        [Trait("spec", "RSL4d")]
        public class WithTextProtocolWithoutEncryption : MockHttpSpecs
        {
            private AblyRest _client;

            private Message GetPayload()
            {
                var payloads = JsonConvert.DeserializeObject<List<Message>>(LastRequest.RequestBody.GetText());
                return payloads.FirstOrDefault();
            }

            public WithTextProtocolWithoutEncryption(ITestOutputHelper output): base(output)
            {
                _client = GetRestClient();
            }

            [Fact]
            [Trait("spec", "RSL4d2")]
            public void WithStringData_DoesNotApplyAnyEncoding()
            {
                //Act
                _client.Channels.Get("Test").Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.data.Should().Be("test");
                payload.encoding.Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RSL4d1")]
            public void WithBinaryData_DoesNotApplyAnyEncoding()
            {
                //Act
                var bytes = new byte[] { 10, 111, 128 };
                _client.Channels.Get("Test").Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                byte[] data = (byte[])payload.data;
                data.ShouldBeEquivalentTo( bytes );
                payload.encoding.Should().Be("base64");
            }

            [Fact]
            [Trait("spec", "RSL4d3")]
            public void WithJsonData_AppliesCorrectEncoding()
            {
                //Arrange
                var obj = new { Test = "test", name = "name" };

                //Act
                _client.Channels.Get("test").Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.data.Should().Be(JsonConvert.SerializeObject(obj));
                payload.encoding.Should().Be("json");
            }
        }

        public class WithTextProtocolWithEncryption : MockHttpSpecs
        {
            private AblyRest _client;
            private ChannelOptions options;

            public WithTextProtocolWithEncryption(ITestOutputHelper output) : base(output)
            {
                options = new ChannelOptions(Crypto.GetDefaultParams());
                _client = GetRestClient();
            }

            private Message GetPayload()
            {
                var payloads = JsonConvert.DeserializeObject<List<Message>>(LastRequest.RequestBody.GetText());
                return payloads.FirstOrDefault();
            }


            [Fact]
            public void WithBinaryData_SetsEncodingAndDataCorrectly()
            {
                //Arrange
                var bytes = new byte[] { 1, 2, 3 };

                //Act
                _client.Channels.Get("test", options).Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("cipher+aes-256-cbc/base64");
                var encryptedBytes = (payload.data as string).FromBase64();
                Crypto.GetCipher(options.CipherParams).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Fact]
            [Trait("spec", "RSL4b")]
            public void WithStringData_SetsEncodingAndDataCorrectly()
            {
                //Act
                _client.Channels.Get("test", options).Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("utf-8/cipher+aes-256-cbc/base64");
                var encryptedBytes = (payload.data as string).FromBase64();
                Crypto.GetCipher(options.CipherParams).Decrypt(encryptedBytes).GetText().Should().BeEquivalentTo("test");
            }

            [Fact]
            public void WithJsonData_SetsEncodingAndDataCorrectly()
            {
                //Act
                var obj = new { Test = "test", Name = "name" };
                _client.Channels.Get("test", options).Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("json/utf-8/cipher+aes-256-cbc/base64");
                var encryptedBytes = (payload.data as string).FromBase64();
                var decryptedString = Crypto.GetCipher(options.CipherParams).Decrypt(encryptedBytes).GetText();
                decryptedString.Should().Be(JsonConvert.SerializeObject(obj));
            }
        }

        [Trait("spec", "RSL4c")]
        public class WithBinaryProtocolWithoutEncryption : MockHttpSpecs
        {
            private AblyRest _client;

            private Message GetPayload()
            {
                using (var stream = new MemoryStream(LastRequest.RequestBody))
                {
                    var context = SerializationContext.Default.GetSerializer<List<Message>>();
                    var payload = context.Unpack(stream).FirstOrDefault();
                    payload.data = ((MessagePackObject)payload.data).ToObject();
                    return payload;
                }
            }

            public WithBinaryProtocolWithoutEncryption(ITestOutputHelper output) : base(output)
            {
                _client = GetRestClient(null, opts => opts.UseBinaryProtocol = true);
            }

            [Fact]
            [Trait("spec", "RSL4c2")]
            public void WithString_DoesNotApplyAnyEncoding()
            {
                //Act
                _client.Channels.Get("Test").Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.data.Should().Be("test");
                payload.encoding.Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RSL4c1")]
            public void WithBinaryData_DoesNotApplyAnyEncoding()
            {
                //Act
                var bytes = new byte[] { 10, 111, 128};
                _client.Channels.Get("Test").Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                (payload.data as byte[]).Should().BeEquivalentTo(bytes);
                payload.encoding.Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RSL4c3")]
            public void WithJsonData_AppliesCorrectEncoding()
            {
                //Arrange
                var obj = new {Test = "test", name = "name"};

                //Act
                _client.Channels.Get("test").Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.data.Should().Be(JsonConvert.SerializeObject(obj));
                payload.encoding.Should().Be("json");
            }
        }

        public class WithBinaryProtocolWithEncryption : MockHttpSpecs
        {
            private AblyRest _client;
            private ChannelOptions options;

            public WithBinaryProtocolWithEncryption(ITestOutputHelper output) : base(output)
            {
                options = new ChannelOptions(Crypto.GetDefaultParams());
                _client = GetRestClient(null, opts => opts.UseBinaryProtocol = true);
            }

            private Message GetPayload()
            {
                using (var stream = new MemoryStream(LastRequest.RequestBody))
                {
                    var context = SerializationContext.Default.GetSerializer<List<Message>>();
                    var payload = context.Unpack(stream).FirstOrDefault();
                    payload.data = ((MessagePackObject) payload.data).ToObject();
                    return payload;
                }
            }

            [Fact]
            public void WithBinaryData_SetsEncodingAndDataCorrectly()
            {
                //Arrange
                var bytes = new byte[] { 1, 2, 3 };

                //Act
                _client.Channels.Get("test", options).Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("cipher+aes-256-cbc");
                var encryptedBytes = (payload.data as byte[]);
                Crypto.GetCipher(options.CipherParams).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Fact]
            public void WithStringData_SetsEncodingAndDataCorrectly()
            {
                //Act
                _client.Channels.Get("test", options).Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("utf-8/cipher+aes-256-cbc");
                var encryptedBytes = (payload.data as byte[]);
                Crypto.GetCipher(options.CipherParams).Decrypt(encryptedBytes).GetText().Should().BeEquivalentTo("test");
            }

            [Fact]
            public void WithJsonData_SetsEncodingAndDataCorrectly()
            {
                //Act
                var obj = new {Test = "test", Name = "name"};
                _client.Channels.Get("test", options).Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("json/utf-8/cipher+aes-256-cbc");
                var encryptedBytes = (payload.data as byte[]);
                var decryptedString = Crypto.GetCipher(options.CipherParams).Decrypt(encryptedBytes).GetText();
                decryptedString.Should().Be(JsonConvert.SerializeObject(obj));
            }
        }

        public MessageEncodersAcceptanceTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}