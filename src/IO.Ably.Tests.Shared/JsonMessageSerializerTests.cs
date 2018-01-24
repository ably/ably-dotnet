﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using IO.Ably.Types;
using Xunit;

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
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(PresenceAction.Enter, null) } }; // 1 empty message
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
                yield return new object[] { "[{}]", new Message[] { new Message(null, null) } };
                yield return new object[] { "[{\"data\":[1234,4321]}]", new Message[] { new Message(null, new JArray(1234, 4321)) } };
            }
        }

        public static IEnumerable<object[]> JsonPresence
        {
            get
            {
                yield return new object[] { "[]", new PresenceMessage[] { } };
                yield return new object[] { "[{\"action\":2,\"clientId\":\"test\"}]", new PresenceMessage[] { new PresenceMessage(PresenceAction.Enter, "test") } };
                yield return new object[] { "[{\"action\":2,\"clientId\":\"test\"}, {\"action\":2,\"clientId\":\"test2\"}]", new PresenceMessage[] { new PresenceMessage(PresenceAction.Enter, "test"), new PresenceMessage(PresenceAction.Enter, "test2") } };
                yield return new object[] { "[{\"connectionId\":\"test\"}]", new PresenceMessage[] { new PresenceMessage() { ConnectionId = "test" } } };
                yield return new object[] { "[{\"data\":\"test\"}]", new PresenceMessage[] { new PresenceMessage() { Data = "test" } } };
                yield return new object[] { "[{\"timestamp\":1430784000000}]", new PresenceMessage[] { new PresenceMessage() { Timestamp = DateHelper.CreateDate(2015, 5, 5) } } };
            }
        }

        private string Serialize(ProtocolMessage message)
        {
            
            return JsonHelper.Serialize(message);
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
            
            ProtocolMessage message = new ProtocolMessage(messageAction);
            string expectedMessage = string.Format("{{\"action\":{0},\"msgSerial\":0}}", (int)messageAction);

            // Act
            Serialize(message).Should().Be(expectedMessage);
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
            
            ProtocolMessage message = new ProtocolMessage() { Channel = channel };
            StringBuilder expectedMessage = new StringBuilder();
            expectedMessage.Append("{\"action\":0");
            if (!string.IsNullOrEmpty(channel))
            {
                expectedMessage.Append(",\"channel\":").AppendFormat("\"{0}\"", channel);
            }
            expectedMessage.Append(",\"msgSerial\":0}");

            // Act & Assert
            Serialize(message).Should().Be(expectedMessage.ToString());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        public void SerializesMessageCorrectly_MsgSerial(long msgSerial)
        {
            // Arrange
            ProtocolMessage message = new ProtocolMessage() { MsgSerial = msgSerial };
            StringBuilder expectedMessage = new StringBuilder();
            expectedMessage.Append("{\"action\":0")
                .AppendFormat(",\"msgSerial\":{0}", msgSerial)
                .Append("}");

            // Act
            Serialize(message).Should().Be(expectedMessage.ToString());

        }

        [Fact]
        public void SerializesMessageCorrectly_NoMessages_DoesNotThrowException()
        {
            // Arrange
            
            ProtocolMessage message = new ProtocolMessage() { Messages = null };

            // Act & Assert
            Serialize(message).Should().Be("{\"action\":0,\"msgSerial\":0}");
        }

        [Theory]
        [MemberData(nameof(Messages))]
        public void SerializesMessageCorrectly_Messages(params Message[] messages)
        {
            // Arrange
            
            ProtocolMessage message = new ProtocolMessage() { Messages = messages };
            StringBuilder expectedMessage = new StringBuilder("{\"action\":0,\"msgSerial\":0");
            var validMessages = messages.Where(c => !string.IsNullOrEmpty(c.Name));
            if (validMessages.Any())
            {
                expectedMessage.Append(",\"messages\":[");
                foreach (Message msg in validMessages)
                {
                    expectedMessage.AppendFormat("{{\"name\":\"{0}\"}},", msg.Name);
                }
                expectedMessage.Remove(expectedMessage.Length - 1, 1) // last comma
                    .Append("]");
            }
            expectedMessage.Append("}");

            // Act & Assert
            Serialize(message).Should().Be(expectedMessage.ToString());
        }

        [Theory]
        [MemberData(nameof(PresenceMessages))]
        public void SerializesMessageCorrectly_Presence(params PresenceMessage[] messages)
        {
            // Arrange
            
            ProtocolMessage message = new ProtocolMessage() { Presence = messages };
            StringBuilder expectedMessage = new StringBuilder("{\"action\":0,\"msgSerial\":0");
            expectedMessage.Append(",\"presence\":[");
            foreach (PresenceMessage msg in messages)
            {
                expectedMessage.AppendFormat("{{\"action\":{0}}},", (byte)msg.Action);
            }
            expectedMessage.Remove(expectedMessage.Length - 1, 1) // last comma
                .Append("]}");

            // Act & Assert
            Serialize(message).Should().Be(expectedMessage.ToString());
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
            
            string message = string.Format("{{ \"action\": {0} }}", (int)action);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(action, target.Action);
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
            
            string message = string.Format("{{\"channel\":{0}}}", channel == null ? "null" : string.Format("\"{0}\"", channel));

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(channel, target.Channel);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_ChannelSerial(string serial)
        {
            // Arrange
            
            string message = string.Format("{{\"channelSerial\":\"{0}\"}}", serial);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(serial, target.ChannelSerial);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_ConnectionId(string connectionId)
        {
            // Arrange
            
            string message = string.Format("{{\"connectionId\":\"{0}\"}}", connectionId);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(connectionId, target.ConnectionId);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_ConnectionKey(string connectionKey)
        {
            // Arrange
            
            string message = string.Format("{{\"connectionKey\":\"{0}\"}}", connectionKey);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(connectionKey, target.ConnectionKey);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_Id(string id)
        {
            // Arrange
            
            string message = string.Format("{{\"id\":\"{0}\"}}", id);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(id, target.Id);
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
            
            string message = string.Format("{{\"connectionSerial\":{0}}}", connectionSerial);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<long>(long.Parse(connectionSerial.ToString(), System.Globalization.CultureInfo.InstalledUICulture), target.ConnectionSerial.Value);
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
            
            string message = string.Format("{{\"count\":{0}}}", count);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<int>(int.Parse(count.ToString(), System.Globalization.CultureInfo.InstalledUICulture), target.Count.Value);
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
            
            string message = string.Format("{{\"msgSerial\":{0}}}", serial);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<long>(long.Parse(serial.ToString(), System.Globalization.CultureInfo.InstalledUICulture), target.MsgSerial);
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
            
            string message = string.Format("{{\"flags\":{0}}}", flags);

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message);

            // Assert
            Assert.NotNull(target);
            Assert.Equal<byte>(byte.Parse(flags.ToString(), System.Globalization.CultureInfo.InstalledUICulture), (byte)target.Flags);
        }

        [Theory]
        [MemberData(nameof(JsonMessages))]
        public void DeserializesMessageCorrectly_Messages(string messageJson, params Message[] expectedMessages)
        {
            // Arrange
            
            StringBuilder message = new StringBuilder("{\"messages\":")
                .Append(messageJson).Append("}");

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message.ToString());

            // Assert
            Assert.NotNull(target);
            Assert.NotNull(target.Messages);
            Assert.Equal<int>(expectedMessages.Length, target.Messages.Length);
            for (int i = 0; i < expectedMessages.Length; i++)
            {
                Assert.Equal(expectedMessages[i].Name, target.Messages[i].Name);
                Assert.Equal(expectedMessages[i].Data, target.Messages[i].Data);
            }
        }

        [Theory]
        [MemberData(nameof(JsonPresence))]
        public void DeserializesMessageCorrectly_Presence(string messageJson, params PresenceMessage[] expectedMessages)
        {
            // Arrange
            
            StringBuilder message = new StringBuilder("{\"presence\":")
                .Append(messageJson).Append("}");

            // Act
            ProtocolMessage target = JsonHelper.Deserialize<ProtocolMessage>(message.ToString());

            // Assert
            Assert.NotNull(target);
            Assert.NotNull(target.Presence);
            Assert.Equal<int>(expectedMessages.Length, target.Presence.Length);
            for (int i = 0; i < expectedMessages.Length; i++)
            {
                Assert.Equal(expectedMessages[i].ClientId, target.Presence[i].ClientId);
                Assert.Equal(expectedMessages[i].ConnectionId, target.Presence[i].ConnectionId);
                Assert.Equal(expectedMessages[i].Action, target.Presence[i].Action);
                Assert.Equal(expectedMessages[i].Id, target.Presence[i].Id);
                Assert.Equal(expectedMessages[i].Timestamp, target.Presence[i].Timestamp);
                Assert.Equal(expectedMessages[i].Data, target.Presence[i].Data);
            }
        }
    }
}
