using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using IO.Ably.Auth;
using IO.Ably.Types;
using MsgPack.Serialization;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions;

namespace IO.Ably.Tests
{
    public class GenerateMsgPackSerializers
    {
        [Fact(Skip="true")]
        public void Generate()
        {
            var applicationLibraryAssembly = typeof(ProtocolMessage).Assembly;
            SerializerGenerator.GenerateCode(
                new SerializerCodeGenerationConfiguration
                {
                    Namespace = "IO.Ably.CustomSerialisers",
                    OutputDirectory = "../../../IO.Ably/CustomSerialisers/GeneratedSerializers",
                    EnumSerializationMethod = EnumSerializationMethod.ByName, // You can tweak it to use ByUnderlyingValue as you like.
                IsRecursive = true, // Set depenendent serializers are also generated.
                PreferReflectionBasedSerializer = false, // Set true if you want to use reflection based collection serializer, false if you want to get generated collection serializers.
                SerializationMethod = SerializationMethod.Map // You tweak it to generate 'map' based serializers.
            },
                applicationLibraryAssembly.GetTypes().Where(type =>
                    type == typeof(Message) || type == typeof(ProtocolMessage) || type == typeof(PresenceMessage) ||
                    type == typeof(PaginatedResult<Stats>) || type == typeof(TokenDetails) || type == typeof(Stats)
                    /* ...you can filter types to be serialized by their namespace, custom attributes, etc... */
                )
            );
        }
    }

    public class MsgPackMessageSerializerTests : AblySpecs
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

        public static IEnumerable<object[]> Presence
        {
            get
            {
                yield return new object[] { new PresenceMessage[] { new PresenceMessage() } }; // 1 empty message
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(), new PresenceMessage() } }; // 2 empty messages
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(), new PresenceMessage(PresenceAction.Enter, "test") } }; // 1 empty, 1 mesage
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(PresenceAction.Enter, "test"), new PresenceMessage(PresenceAction.Enter, "test2") } }; // 2 messages
            }
        }

        public static IEnumerable<object[]> BinMessages
        {
            get
            {
                yield return new object[] { new byte[] { 0x90 }, new Message[] { } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x6e, 0x61, 0x6d, 0x65, 0xa4, 0x74, 0x65, 0x73, 0x74 }, new Message[] { new Message("test", null) } };
                yield return new object[] { new byte[] { 0x92, 0x81, 0xa4, 0x6e, 0x61, 0x6d, 0x65, 0xa4, 0x74, 0x65, 0x73, 0x74, 0x81, 0xa4, 0x6e, 0x61, 0x6d, 0x65, 0xa6, 0x61, 0x74, 0x74, 0x61, 0x63, 0x68 }, new Message[] { new Message("test", null), new Message("attach", null) } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x64, 0x61, 0x74, 0x61, 0xa4, 0x74, 0x65, 0x73, 0x74 }, new Message[] { new Message(null, "test") } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x64, 0x61, 0x74, 0x61, 0xcd, 0x04, 0xd2 }, new Message[] { new Message(null, (ushort)1234) } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x64, 0x61, 0x74, 0x61, 0xcb, 0x40, 0x5e, 0xc0, 0xa3, 0xd7, 0x0a, 0x3d, 0x71 }, new Message[] { new Message(null, 123.01d) } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x64, 0x61, 0x74, 0x61, 0xc3 }, new Message[] { new Message(null, true) } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x64, 0x61, 0x74, 0x61, 0xc0 }, new Message[] { new Message(null, null) } };
                yield return new object[] { new byte[] { 0x91, 0x81, 0xa4, 0x64, 0x61, 0x74, 0x61, 0x92, 0xcd, 0x04, 0xd2, 0xcd, 0x10, 0xe1 }, new Message[] { new Message(null, new object[] { (ushort)1234, (ushort)4321 }) } };
            }
        }

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
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x82);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add((byte)messageAction);
            expectedMessage.AddRange(SerializeString("msgSerial"));
            expectedMessage.Add(0);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            Assert.IsType<byte[]>(result);
            Output.WriteLine(expectedMessage.ToArray().GetText());
            Output.WriteLine((result as byte[]).GetText());
            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
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
            ProtocolMessage message = new ProtocolMessage() { channel = channel };
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x82);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add(0);
            if (!string.IsNullOrEmpty(channel))
            {
                expectedMessage[0]++;
                expectedMessage.AddRange(SerializeString("channel"));
                expectedMessage.AddRange(SerializeString(channel));
            }
            expectedMessage.AddRange(SerializeString("msgSerial"));
            expectedMessage.Add(0);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            Assert.IsType<byte[]>(result);
            Output.WriteLine(expectedMessage.ToArray().GetText());
            Output.WriteLine((result as byte[]).GetText());
            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        public void SerializesMessageCorrectly_MsgSerial(long msgSerial)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { MsgSerial = msgSerial };
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x82);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add(0);
            expectedMessage.AddRange(SerializeString("msgSerial"));
            if (Math.Abs(msgSerial) < 255)
            {
                expectedMessage.Add(BitConverter.GetBytes(msgSerial).First());
            }
            else
            {
                expectedMessage.Add(0xd1);
                expectedMessage.AddRange(BitConverter.GetBytes(msgSerial).TakeWhile(c => c > 0).Reverse());
            }

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<byte[]>(result);
            Output.WriteLine(expectedMessage.ToArray().GetText());
            Output.WriteLine((result as byte[]).GetText());
            Assert.Equal<byte[]>(expectedMessage.ToArray(), result as byte[]);
        }

        [Theory]
        [MemberData("Messages")]
        public void SerializesMessageCorrectly_Messages(params Message[] messages)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { messages = messages };
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x82);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add(0);
            expectedMessage.AddRange(SerializeString("msgSerial"));
            expectedMessage.Add(0);
            var validMessages = messages.Where(c => !string.IsNullOrEmpty(c.name));
            if (validMessages.Any())
            {
                expectedMessage[0]++;
                expectedMessage.AddRange(SerializeString("messages"));
                expectedMessage.Add((byte)((0x09 << 4) + validMessages.Count()));
                foreach (Message msg in validMessages)
                {
                    expectedMessage.Add((0x08 << 4) + 1);
                    expectedMessage.AddRange(SerializeString("name"));
                    expectedMessage.AddRange(SerializeString(msg.name));
                }
            }

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<byte[]>(result);
            Output.WriteLine(expectedMessage.ToArray().GetText());
            Output.WriteLine((result as byte[]).GetText());
            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
        }

        [Theory]
        [MemberData("Presence")]
        public void SerializesMessageCorrectly_Presence(params PresenceMessage[] messages)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            ProtocolMessage message = new ProtocolMessage() { presence = messages };
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x82);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add(0);
            expectedMessage.AddRange(SerializeString("msgSerial"));
            expectedMessage.Add(0);
            if (messages.Length > 0)
            {
                expectedMessage[0]++;
                expectedMessage.AddRange(SerializeString("presence"));
                expectedMessage.Add((byte)((0x09 << 4) + messages.Length));
                foreach (PresenceMessage msg in messages)
                {
                    expectedMessage.Add((0x08 << 4) + 1);
                    expectedMessage[expectedMessage.Count - 1] += (byte)(string.IsNullOrEmpty(msg.clientId) ? 0 : 1);
                    expectedMessage.AddRange(SerializeString("action"));
                    expectedMessage.Add((byte)msg.action);
                    if (!string.IsNullOrEmpty(msg.clientId))
                    {
                        expectedMessage.AddRange(SerializeString("clientId"));
                        expectedMessage.AddRange(SerializeString(msg.clientId));
                    }
                }
            }

            // Act
            object result = serializer.SerializeProtocolMessage(message);

            // Assert
            Assert.IsType<byte[]>(result);
            Output.WriteLine(expectedMessage.ToArray().GetText());
            Output.WriteLine((result as byte[]).GetText());
            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
        }

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
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add((byte)action);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

            // Assert
            Assert.NotNull(target);
            Assert.Equal(action, target.action);
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
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("channel"));
            if (channel != null)
            {
                expectedMessage.AddRange(SerializeString(channel));
            }
            else
            {
                expectedMessage.Add(0xc0);
            }

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

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
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("channelSerial"));
            if (serial != null)
            {
                expectedMessage.AddRange(SerializeString(serial));
            }
            else
            {
                expectedMessage.Add(0xc0);
            }

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

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
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("connectionId"));
            expectedMessage.AddRange(SerializeString(connectionId));

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

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
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("connectionKey"));
            expectedMessage.AddRange(SerializeString(connectionKey));

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

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
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("id"));
            expectedMessage.AddRange(SerializeString(id));

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

            // Assert
            Assert.NotNull(target);
            Assert.Equal<string>(id, target.id);
        }

        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeserializesMessageCorrectly_ConnectionSerial(long connectionSerial)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("connectionSerial"));
            expectedMessage.Add(BitConverter.GetBytes(connectionSerial).First());

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

            // Assert
            Assert.NotNull(target);
            Assert.Equal<long>(connectionSerial, target.connectionSerial.Value);
        }

        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeserializesMessageCorrectly_Count(int count)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            byte[] expectedMessage = serializer.SerializeProtocolMessage(new ProtocolMessage() {count = count}) as byte[];

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage);

            // Assert
            Assert.NotNull(target);
            Assert.Equal(count, target.count.Value);
        }

        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeserializesMessageCorrectly_MsgSerial(long serial)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("msgSerial"));
            expectedMessage.Add(BitConverter.GetBytes(serial).First());

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

            // Assert
            Assert.NotNull(target);
            Assert.Equal<long>(serial, target.MsgSerial);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(123)]
        public void DeserializesMessageCorrectly_Flags(int flags)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("flags"));
            expectedMessage.Add((byte)flags);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

            // Assert
            Assert.NotNull(target);
            Assert.Equal<byte>((byte)flags, (byte)target.flags);
        }

        [Theory]
        [MemberData("BinMessages")]
        public void DeserializesMessageCorrectly_Messages(byte[] messageBin, params Message[] expectedMessages)
        {
            // Arrange
            MsgPackMessageSerializer serializer = new MsgPackMessageSerializer();
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("messages"));
            expectedMessage.AddRange(messageBin);

            // Act
            ProtocolMessage target = serializer.DeserializeProtocolMessage(expectedMessage.ToArray());

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

        private static byte[] SerializeString(string str)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)(0xa0 + str.Length));
            bytes.AddRange(System.Text.Encoding.GetEncoding("utf-8").GetBytes(str));
            return bytes.ToArray();
        }

        public MsgPackMessageSerializerTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
