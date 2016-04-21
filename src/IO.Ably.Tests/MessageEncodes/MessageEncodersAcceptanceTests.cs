using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using IO.Ably.Encryption;
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
                payload.encoding.Should().Be("cipher+aes-128-cbc/base64");
                var encryptedBytes = (payload.data as string).FromBase64();
                Crypto.GetCipher(options).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Fact]
            public void WithStringData_SetsEncodingAndDataCorrectly()
            {
                //Act
                _client.Channels.Get("test", options).Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("utf-8/cipher+aes-128-cbc/base64");
                var encryptedBytes = (payload.data as string).FromBase64();
                Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText().Should().BeEquivalentTo("test");
            }

            [Fact]
            public void WithJsonData_SetsEncodingAndDataCorrectly()
            {
                //Act
                var obj = new { Test = "test", Name = "name" };
                _client.Channels.Get("test", options).Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("json/utf-8/cipher+aes-128-cbc/base64");
                var encryptedBytes = (payload.data as string).FromBase64();
                var decryptedString = Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText();
                decryptedString.Should().Be(JsonConvert.SerializeObject(obj));
            }
        }

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
                payload.encoding.Should().Be("cipher+aes-128-cbc");
                var encryptedBytes = (payload.data as byte[]);
                Crypto.GetCipher(options).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Fact]
            public void WithStringData_SetsEncodingAndDataCorrectly()
            {
                //Act
                _client.Channels.Get("test", options).Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("utf-8/cipher+aes-128-cbc");
                var encryptedBytes = (payload.data as byte[]);
                Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText().Should().BeEquivalentTo("test");
            }

            [Fact]
            public void WithJsonData_SetsEncodingAndDataCorrectly()
            {
                //Act
                var obj = new {Test = "test", Name = "name"};
                _client.Channels.Get("test", options).Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.encoding.Should().Be("json/utf-8/cipher+aes-128-cbc");
                var encryptedBytes = (payload.data as byte[]);
                var decryptedString = Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText();
                decryptedString.Should().Be(JsonConvert.SerializeObject(obj));
            }
        }

        public MessageEncodersAcceptanceTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}