using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using FluentAssertions;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Ably.AcceptanceTests
{
    public class ChannelAcceptanceTests
    {
        
    }

    [TestFixture]
    public class RestAcceptanceTests
    {
        [Test]
        public void ShouldReturnTheRequestedToken()
        {
            //Arrange
            var fakeKey = "AppId.KeyId:KeyValue";
            var ably = new Rest(new AblyOptions() { Key = fakeKey, Environment = AblyEnvironment.Sandbox });

            //Act
            var error = Assert.Throws<AblyException>(delegate { ably.Channels.Get("Test").Publish("test", true); });

            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            error.ErrorInfo.Code.Should().Be(40100);
        }
    }


    public class MessageEncodersAcceptanceTests
    {
        private static string fakeKey = "AppId.KeyId:KeyValue";

        [TestFixture]
        public class WithBinaryProtocolWithoutEncryption
        {
            private Rest _client;
            private AblyRequest currentRequest;

            private MessagePayload GetPayload()
            {
                using (var stream = new MemoryStream(currentRequest.RequestBody))
                {
                    var context = SerializationContext.Default.GetSerializer<List<MessagePayload>>();
                    var payload = context.Unpack(stream).FirstOrDefault();
                    payload.data = ((MessagePackObject)payload.data).ToObject();
                    return payload;
                }
            }

            public WithBinaryProtocolWithoutEncryption()
            {
                _client = new Rest(new AblyOptions() { Key = fakeKey, Protocol = Protocol.MsgPack});
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
                payload.data.Should().Be("test");
                payload.encoding.Should().BeNull();
            }

            [Test]
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

            [Test]
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

        [TestFixture]
        public class WithBinaryProtocolWithEncryption
        {
            private Rest _client;
            private AblyRequest currentRequest;
             private ChannelOptions options;

             public WithBinaryProtocolWithEncryption()
             {
                 options = new ChannelOptions(Crypto.GetDefaultParams());
                _client = new Rest(new AblyOptions() { Key = fakeKey, Protocol = Protocol.MsgPack});
                _client.ExecuteHttpRequest = request =>
                {
                    currentRequest = request;
                    return new AblyResponse() { TextResponse = "{}"};
                };
             }

            private MessagePayload GetPayload()
            {
                using (var stream = new MemoryStream(currentRequest.RequestBody))
                {
                    var context = SerializationContext.Default.GetSerializer<List<MessagePayload>>();
                    var payload = context.Unpack(stream).FirstOrDefault();
                    payload.data = ((MessagePackObject) payload.data).ToObject();
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
                payload.encoding.Should().Be("cipher+aes-128-cbc");
                var encryptedBytes = (payload.data as byte[]);
                Crypto.GetCipher(options).Decrypt(encryptedBytes).Should().BeEquivalentTo(bytes);
            }

            [Test]
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

            [Test]
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
    }
}
