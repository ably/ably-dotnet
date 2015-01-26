using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FluentAssertions;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Ably.AcceptanceTests
{
    public class MessageEncodersAcceptanceTests
    {
        private static string fakeKey = "AppId.KeyId:KeyValue";

        [TestFixture]
        public class WithTextProtocolWithoutEncryption
        {
            private Rest _client;
            private AblyRequest currentRequest;

            private Message GetPayload()
            {
                var payloads = JsonConvert.DeserializeObject<List<Message>>(currentRequest.RequestBody.GetText());
                return payloads.FirstOrDefault();
            }

            public WithTextProtocolWithoutEncryption()
            {
                _client = new Rest(new AblyOptions() { Key = fakeKey, UseBinaryProtocol = false});
                _client.ExecuteHttpRequest = request =>
                {
                    currentRequest = request;
                    return new AblyResponse() { TextResponse = "{}"};
                };
            }

            [Test]
            public void WithStringData_DoesNotApplyAnyEncoding()
            {
                //Act
                _client.Channels.Get("Test").Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.Data.Should().Be("test");
                payload.Encoding.Should().BeNull();
            }

            [Test]
            public void WithBinaryData_DoesNotApplyAnyEncoding()
            {
                //Act
                var bytes = new byte[] { 10, 111, 128 };
                _client.Channels.Get("Test").Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                (payload.Data as string).FromBase64().Should().BeEquivalentTo(bytes);
                payload.Encoding.Should().Be("base64");
            }

            [Test]
            public void WithJsonData_AppliesCorrectEncoding()
            {
                //Arrange
                var obj = new { Test = "test", name = "name" };

                //Act
                _client.Channels.Get("test").Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.Data.Should().Be(JsonConvert.SerializeObject(obj));
                payload.Encoding.Should().Be("json");
            }
        }

        [TestFixture]
        public class WithTextProtocolWithEncryption
        {
            private Rest _client;
            private AblyRequest currentRequest;
            private ChannelOptions options;

            public WithTextProtocolWithEncryption()
            {
                options = new ChannelOptions(Crypto.GetDefaultParams());
                _client = new Rest(new AblyOptions() { Key = fakeKey, UseBinaryProtocol = false});
                _client.ExecuteHttpRequest = request =>
                {
                    currentRequest = request;
                    return new AblyResponse() { TextResponse = "{}"};
                };
            }

            private Message GetPayload()
            {
                var payloads = JsonConvert.DeserializeObject<List<Message>>(currentRequest.RequestBody.GetText());
                return payloads.FirstOrDefault();
            }


            [Test]
            public void WithBinaryData_SetsEncodingAndDataCorrectly()
            {
                //Arrange
                var bytes = new byte[] { 1, 2, 3 };

                //Act
                _client.Channels.Get("test", options).Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                payload.Encoding.Should().Be("cipher+aes-128-cbc/base64");
                var encryptedBytes = (payload.Data as string).FromBase64();
                Crypto.GetCipher(options).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Test]
            public void WithStringData_SetsEncodingAndDataCorrectly()
            {
                //Act
                _client.Channels.Get("test", options).Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.Encoding.Should().Be("utf-8/cipher+aes-128-cbc/base64");
                var encryptedBytes = (payload.Data as string).FromBase64();
                Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText().Should().BeEquivalentTo("test");
            }

            [Test]
            public void WithJsonData_SetsEncodingAndDataCorrectly()
            {
                //Act
                var obj = new { Test = "test", Name = "name" };
                _client.Channels.Get("test", options).Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.Encoding.Should().Be("json/utf-8/cipher+aes-128-cbc/base64");
                var encryptedBytes = (payload.Data as string).FromBase64();
                var decryptedString = Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText();
                decryptedString.Should().Be(JsonConvert.SerializeObject(obj));
            }
        }

        [TestFixture]
        public class WithBinaryProtocolWithoutEncryption
        {
            private Rest _client;
            private AblyRequest currentRequest;

            private Message GetPayload()
            {
                using (var stream = new MemoryStream(currentRequest.RequestBody))
                {
                    var context = SerializationContext.Default.GetSerializer<List<Message>>();
                    var payload = context.Unpack(stream).FirstOrDefault();
                    payload.Data = ((MessagePackObject)payload.Data).ToObject();
                    return payload;
                }
            }

            public WithBinaryProtocolWithoutEncryption()
            {
                _client = new Rest(new AblyOptions() { Key = fakeKey, UseBinaryProtocol = true});
                _client.ExecuteHttpRequest = request =>
                {
                    currentRequest = request;
                    return new AblyResponse() { TextResponse = "{}"};
                };
            }

            [Test]
            public void WithString_DoesNotApplyAnyEncoding()
            {
                //Act
                _client.Channels.Get("Test").Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.Data.Should().Be("test");
                payload.Encoding.Should().BeNull();
            }

            [Test]
            public void WithBinaryData_DoesNotApplyAnyEncoding()
            {
                //Act
                var bytes = new byte[] { 10, 111, 128};
                _client.Channels.Get("Test").Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                (payload.Data as byte[]).Should().BeEquivalentTo(bytes);
                payload.Encoding.Should().BeNull();
            }

            [Test]
            public void WithJsonData_AppliesCorrectEncoding()
            {
                //Arrange
                var obj = new {Test = "test", name = "name"};

                //Act
                _client.Channels.Get("test").Publish("test", obj);
                
                //Assert
                var payload = GetPayload();
                payload.Data.Should().Be(JsonConvert.SerializeObject(obj));
                payload.Encoding.Should().Be("json");
            }
        }

        [TestFixture]
        public class WithBinaryProtocolWithEncryption
        {
            private Rest _client;
            private AblyRequest currentRequest;
            private ChannelOptions options;

            public WithBinaryProtocolWithEncryption()
            {
                options = new ChannelOptions(Crypto.GetDefaultParams());
                _client = new Rest(new AblyOptions() { Key = fakeKey, UseBinaryProtocol = true});
                _client.ExecuteHttpRequest = request =>
                {
                    currentRequest = request;
                    return new AblyResponse() { TextResponse = "{}"};
                };
            }

            private Message GetPayload()
            {
                using (var stream = new MemoryStream(currentRequest.RequestBody))
                {
                    var context = SerializationContext.Default.GetSerializer<List<Message>>();
                    var payload = context.Unpack(stream).FirstOrDefault();
                    payload.Data = ((MessagePackObject) payload.Data).ToObject();
                    return payload;
                }
            }

            [Test]
            public void WithBinaryData_SetsEncodingAndDataCorrectly()
            {
                //Arrange
                var bytes = new byte[] { 1, 2, 3 };

                //Act
                _client.Channels.Get("test", options).Publish("test", bytes);

                //Assert
                var payload = GetPayload();
                payload.Encoding.Should().Be("cipher+aes-128-cbc");
                var encryptedBytes = (payload.Data as byte[]);
                Crypto.GetCipher(options).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Test]
            public void WithStringData_SetsEncodingAndDataCorrectly()
            {
                //Act
                _client.Channels.Get("test", options).Publish("test", "test");

                //Assert
                var payload = GetPayload();
                payload.Encoding.Should().Be("utf-8/cipher+aes-128-cbc");
                var encryptedBytes = (payload.Data as byte[]);
                Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText().Should().BeEquivalentTo("test");
            }

            [Test]
            public void WithJsonData_SetsEncodingAndDataCorrectly()
            {
                //Act
                var obj = new {Test = "test", Name = "name"};
                _client.Channels.Get("test", options).Publish("test", obj);

                //Assert
                var payload = GetPayload();
                payload.Encoding.Should().Be("json/utf-8/cipher+aes-128-cbc");
                var encryptedBytes = (payload.Data as byte[]);
                var decryptedString = Crypto.GetCipher(options).Decrypt(encryptedBytes).GetText();
                decryptedString.Should().Be(JsonConvert.SerializeObject(obj));
            }
        }
    }
}