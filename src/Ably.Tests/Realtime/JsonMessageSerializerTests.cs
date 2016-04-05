using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IO.Ably.Types;
using Xunit;
using Xunit.Extensions;

namespace IO.Ably.Tests
{
    public class JsonMessageSerializerTests
    {
        public static IEnumerable<object[]> Messages
        {
            get
            {
                yield return new object[] { new Message[] { new Message() } }; // 1 empty message
                yield return new object[] { new Message[] { new Message(), new Message() } }; // 2 empty messages
                yield return new object[] { new Message[] { new Message(), new Message("test", null) } }; // 1 empty, 1 mesage
                yield return new object[] { new Message[] { new Message("test", null), new Message("attach", null) } }; // 2 messages
            }
        }

        public static IEnumerable<object[]> PresenceMessages
        {
            get
            {
                yield return new object[] { new PresenceMessage[] { new PresenceMessage() } }; // 1 empty message
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, null) } }; // 1 empty message
            }
        }

        public static IEnumerable<object[]> JsonMessages
        {
            get
            {
                yield return new object[] { "[]", new Message[] { } };
                yield return new object[] { "[{\"name\":\"test\"}]", new Message[] { new Message("test", null) } };
                yield return new object[] { "[{\"name\":\"test\"},{\"name\":\"attach\"}]", new Message[] { new Message("test", null),  new Message("attach", null) } };
                yield return new object[] { "[{\"data\":\"test\"}]", new Message[] { new Message(null, "test") } };
                yield return new object[] { "[{\"data\":\"2012-04-23T18:25:43.511Z\"}]", new Message[] { new Message(null, new DateTime(2012, 4, 23, 18, 25, 43, 511)) } };
                yield return new object[] { "[{\"data\":1234}]", new Message[] { new Message(null, 1234) } };
                yield return new object[] { "[{\"data\":1234.00}]", new Message[] { new Message(null, 1234f) } };
                yield return new object[] { "[{\"data\":true}]", new Message[] { new Message(null, true) } };
                yield return new object[] { "[{\"data\":undefined}]", new Message[] { new Message(null, null) } };
                yield return new object[] { "[{\"data\":[1234,4321]}]", new Message[] { new Message(null, new JArray(1234, 4321)) } };
                yield return new object[] { "[{\"data\":\"bXkgYmluYXJ5IHBheWxvYWQ=\",\"encoding\":\"base64\"}]", new Message[] { new Message(null, Convert.FromBase64String("bXkgYmluYXJ5IHBheWxvYWQ=")) } };
            }
        }

        public static IEnumerable<object[]> JsonPresence
        {
            get
            {
                yield return new object[] { "[]", new PresenceMessage[] { } };
                yield return new object[] { "[{\"action\":2,\"clientId\":\"test\"}]", new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "test") } };
                yield return new object[] { "[{\"action\":2,\"clientId\":\"test\"}, {\"action\":2,\"clientId\":\"test2\"}]", new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "test"), new PresenceMessage(PresenceMessage.ActionType.Enter, "test2") } };
                yield return new object[] { "[{\"connectionId\":\"test\"}]", new PresenceMessage[] { new PresenceMessage() { connectionId = "test" } } };
                yield return new object[] { "[{\"data\":\"test\"}]", new PresenceMessage[] { new PresenceMessage() { data = "test" } } };
                yield return new object[] { "[{\"timestamp\":1430784000000}]", new PresenceMessage[] { new PresenceMessage() { timestamp = new DateTime(2015, 5, 5, 0, 0, 0, DateTimeKind.Utc) } } };
            }
        }

        //
        // Serialization tests
        //

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void SerializesMessageCorrectly_Action(ProtocolMessage.MessageAction messageAction)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            ProtocolMessage message = new ProtocolMessage(messageAction);
            string expectedMessage = string.Format("{{\"action\":{0},\"msgSerial\":0}}", (int)messageAction);

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal<string>(expectedMessage, result as string);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("test")]
        [InlineData("my channel")]
        [InlineData("1234")]
        public void SerializesMessageCorrectly_Channel(string channel)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { channel = channel };
            StringBuilder expectedMessage = new StringBuilder();
            expectedMessage.Append("{\"action\":0");
            if (!string.IsNullOrEmpty(channel))
            {
                expectedMessage.Append(",\"channel\":").AppendFormat("\"{0}\"", channel);
            }
            expectedMessage.Append(",\"msgSerial\":0}");

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal<string>(expectedMessage.ToString(), result as string);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        public void SerializesMessageCorrectly_MsgSerial(long msgSerial)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { msgSerial = msgSerial };
            StringBuilder expectedMessage = new StringBuilder();
            expectedMessage.Append("{\"action\":0")
                .AppendFormat(",\"msgSerial\":{0}", msgSerial)
                .Append("}");

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal<string>(expectedMessage.ToString(), result as string);
        }

        [Fact]
        public void SerializesMessageCorrectly_NoMessages_DoesNotThrowException()
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { messages = null };

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal<string>("{\"action\":0,\"msgSerial\":0}", result as string);
        }

        [Theory]
        [MemberData("Messages")]
        public void SerializesMessageCorrectly_Messages(params Message[] messages)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { messages = messages };
            StringBuilder expectedMessage = new StringBuilder("{\"action\":0,\"msgSerial\":0");
            var validMessages = messages.Where(c => !string.IsNullOrEmpty(c.name));
            if (validMessages.Any())
            {
                expectedMessage.Append(",\"messages\":[");
                foreach (Message msg in validMessages)
                {
                    expectedMessage.AppendFormat("{{\"name\":\"{0}\"}},", msg.name);
                }
                expectedMessage.Remove(expectedMessage.Length - 1, 1) // last comma
                    .Append("]");
            }
            expectedMessage.Append("}");

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal<string>(expectedMessage.ToString(), result as string);
        }

        [Theory]
        [MemberData("PresenceMessages")]
        public void SerializesMessageCorrectly_Presence(params PresenceMessage[] messages)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { presence = messages };
            StringBuilder expectedMessage = new StringBuilder("{\"action\":0,\"msgSerial\":0");
            expectedMessage.Append(",\"presence\":[");
            foreach (PresenceMessage msg in messages)
            {
                expectedMessage.AppendFormat("{{\"action\":{0}}},", (byte)msg.action);
            }
            expectedMessage.Remove(expectedMessage.Length - 1, 1) // last comma
                .Append("]}");

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal<string>(expectedMessage.ToString(), result as string);
        }

        //
        // Deserialization tests
        //

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void DeserializesMessageCorrectly_Action(ProtocolMessage.MessageAction action)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"action\":{0}}}", (int)action);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<ProtocolMessage.MessageAction>(action, target.action);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("test")]
        [InlineData("my channel")]
        [InlineData("1234")]
        public void DeserializesMessageCorrectly_Channel(string channel)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"channel\":{0}}}", channel == null ? "null" : string.Format("\"{0}\"", channel));

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<string>(channel, target.channel);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_ChannelSerial(string serial)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"channelSerial\":\"{0}\"}}", serial);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<string>(serial, target.channelSerial);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_ConnectionId(string connectionId)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"connectionId\":\"{0}\"}}", connectionId);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<string>(connectionId, target.connectionId);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_ConnectionKey(string connectionKey)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"connectionKey\":\"{0}\"}}", connectionKey);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<string>(connectionKey, target.connectionKey);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_Id(string id)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"id\":\"{0}\"}}", id);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<string>(id, target.id);
        }

        [Theory]
        [InlineData(123)]
        [InlineData("123")]
        [InlineData(0)]
        [InlineData("0")]
        [InlineData(-1)]
        [InlineData("-1")]
        public void DeserializesMessageCorrectly_ConnectionSerial(object connectionSerial)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"connectionSerial\":{0}}}", connectionSerial);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<long>(long.Parse(connectionSerial.ToString(), System.Globalization.CultureInfo.InstalledUICulture), target.connectionSerial.Value);
        }

        [Theory]
        [InlineData(123)]
        [InlineData("123")]
        [InlineData(0)]
        [InlineData("0")]
        [InlineData(-1)]
        [InlineData("-1")]
        public void DeserializesMessageCorrectly_Count(object count)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"count\":{0}}}", count);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<int>(int.Parse(count.ToString(), System.Globalization.CultureInfo.InstalledUICulture), target.count);
        }

        [Theory]
        [InlineData(123)]
        [InlineData("123")]
        [InlineData(0)]
        [InlineData("0")]
        [InlineData(-1)]
        [InlineData("-1")]
        public void DeserializesMessageCorrectly_MsgSerial(object serial)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"msgSerial\":{0}}}", serial);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<long>(long.Parse(serial.ToString(), System.Globalization.CultureInfo.InstalledUICulture), target.msgSerial);
        }

        [Theory]
        [InlineData(0)]
        [InlineData("0")]
        [InlineData(1)]
        [InlineData("1")]
        [InlineData(123)]
        [InlineData("123")]
        public void DeserializesMessageCorrectly_Flags(object flags)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            string message = string.Format("{{\"flags\":{0}}}", flags);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<byte>(byte.Parse(flags.ToString(), System.Globalization.CultureInfo.InstalledUICulture), (byte)target.flags);
        }

        [Theory]
        [MemberData("JsonMessages")]
        public void DeserializesMessageCorrectly_Messages(string messageJson, params Message[] expectedMessages)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            StringBuilder message = new StringBuilder("{\"messages\":")
                .Append(messageJson).Append("}");

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message.ToString());

            // Assert
            Assert.NotNull(target);
            Assert.NotNull(target.messages);
            Assert.Equal<int>(expectedMessages.Length, target.messages.Length);
            for (int i = 0; i < expectedMessages.Length; i++)
            {
                Assert.Equal<string>(expectedMessages[i].name, target.messages[i].name);
                Assert.Equal(expectedMessages[i].data, target.messages[i].data);
            }
        }

        [Theory]
        [MemberData("JsonPresence")]
        public void DeserializesMessageCorrectly_Presence(string messageJson, params PresenceMessage[] expectedMessages)
        {
            // Arrange
            JsonMessageSerializer serializer = new JsonMessageSerializer();
            StringBuilder message = new StringBuilder("{\"presence\":")
                .Append(messageJson).Append("}");

            Console.WriteLine(new DateTime(2015, 5, 5, 0, 0, 0, DateTimeKind.Utc).ToUnixTimeInMilliseconds());

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(message.ToString());

            // Assert
            Assert.NotNull(target);
            Assert.NotNull(target.presence);
            Assert.Equal<int>(expectedMessages.Length, target.presence.Length);
            for (int i = 0; i < expectedMessages.Length; i++)
            {
                Assert.Equal<string>(expectedMessages[i].clientId, target.presence[i].clientId);
                Assert.Equal<string>(expectedMessages[i].connectionId, target.presence[i].connectionId);
                Assert.Equal<PresenceMessage.ActionType>(expectedMessages[i].action, target.presence[i].action);
                Assert.Equal<string>(expectedMessages[i].id, target.presence[i].id);
                Assert.Equal<DateTime>(expectedMessages[i].timestamp, target.presence[i].timestamp);
                Assert.Equal(expectedMessages[i].data, target.presence[i].data);
            }
        }
    }
}
