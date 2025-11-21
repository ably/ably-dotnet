using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IO.Ably.Tests.Shared.Helpers;
using IO.Ably.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.MsgPack
{
    // NOTE: The old GenerateMsgPackSerializers class has been removed.
    // MessagePack-CSharp v3.x uses automatic source generation during build.
    // No manual serializer generation is needed - the source generator handles it automatically.

    public class MsgPackMessageSerializerTests : AblySpecs
    {
        public static IEnumerable<object[]> Messages
        {
            get
            {
                yield return new object[] { new Message[] { new Message() } }; // 1 empty message
                yield return new object[] { new Message[] { new Message(), new Message() } }; // 2 empty messages
                yield return new object[] { new Message[] { new Message(), new Message("test", null) } }; // 1 empty, 1 message
                yield return new object[] { new Message[] { new Message("test", null), new Message("attach", null) } }; // 2 messages
            }
        }

        public static IEnumerable<object[]> Presence
        {
            get
            {
                yield return new object[] { new PresenceMessage[] { new PresenceMessage() } }; // 1 empty message
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(), new PresenceMessage() } }; // 2 empty messages
                yield return new object[] { new PresenceMessage[] { new PresenceMessage(), new PresenceMessage(PresenceAction.Enter, "test") } }; // 1 empty, 1 message
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
            List<byte> expectedMessage = BuildExpectedProtocolMessage(action: messageAction);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            result.Should().BeOfType<byte[]>();

            ValidateAndLog(expectedMessage.ToArray(), result as byte[]);

            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
        }

        private void ValidateAndLog(byte[] expectedBytes, byte[] actualBytes)
        {
            if (!expectedBytes.SequenceEqual(actualBytes))
            {
                Output.WriteLine($"Expected: {BitConverter.ToString(expectedBytes)}");
                Output.WriteLine($"Actual:   {BitConverter.ToString(actualBytes)}");
            }
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
            List<byte> expectedMessage = BuildExpectedProtocolMessage(channel: channel);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            result.Should().BeOfType<byte[]>();

            ValidateAndLog(expectedMessage.ToArray(), result as byte[]);

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
            ProtocolMessage message = new ProtocolMessage() { MsgSerial = msgSerial };
            List<byte> expectedMessage = BuildExpectedProtocolMessage(msgSerial: msgSerial);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            result.Should().BeOfType<byte[]>();

            ValidateAndLog(expectedMessage.ToArray(), result as byte[]);

            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
        }

        [Theory]
        [MemberData(nameof(Messages))]
        public void SerializesMessageCorrectly_Messages(params Message[] messages)
        {
            // Arrange
            ProtocolMessage message = new ProtocolMessage() { Messages = messages };
            List<byte> expectedMessage = BuildExpectedProtocolMessage(messages: messages);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            result.Should().BeOfType<byte[]>();

            ValidateAndLog(expectedMessage.ToArray(), result as byte[]);

            Assert.Equal(expectedMessage.ToArray(), result as byte[]);
        }

        [Theory]
        [MemberData(nameof(Presence))]
        public void SerializesMessageCorrectly_Presence(params PresenceMessage[] messages)
        {
            // Arrange
            ProtocolMessage message = new ProtocolMessage() { Presence = messages };
            List<byte> expectedMessage = BuildExpectedProtocolMessage(presence: messages);

            // Act
            object result = MsgPackHelper.Serialise(message);

            // Assert
            result.Should().BeOfType<byte[]>();

            ValidateAndLog(expectedMessage.ToArray(), result as byte[]);

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
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add((byte)action);

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
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
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
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
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
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
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("connectionId"));
            expectedMessage.AddRange(SerializeString(connectionId));

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
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
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("connectionKey"));
            expectedMessage.AddRange(SerializeString(connectionKey));

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
            // Assert.Equal(connectionKey, target.ConnectionKey);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("123^&456")]
        [InlineData("absder#^&456")]
        public void DeserializesMessageCorrectly_Id(string id)
        {
            // Arrange
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("id"));
            expectedMessage.AddRange(SerializeString(id));

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
            Assert.Equal(id, target.Id);
        }

        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeserializesMessageCorrectly_Count(int count)
        {
            // Arrange
            byte[] expectedMessage = MsgPackHelper.Serialise(new ProtocolMessage() { Count = count }) as byte[];

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage);

            // Assert
            target.Should().NotBeNull();
            Assert.Equal(count, target.Count.Value);
        }

        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-1)]
        public void DeserializesMessageCorrectly_MsgSerial(long serial)
        {
            // Arrange
            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("msgSerial"));
            expectedMessage.Add(BitConverter.GetBytes(serial).First());

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
            Assert.Equal<long>(serial, target.MsgSerial);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(123)]
        public void DeserializesMessageCorrectly_Flags(int flags)
        {
            // Arrange

            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("flags"));
            expectedMessage.Add((byte)flags);

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
            Assert.Equal<byte>((byte)flags, (byte)target.Flags);
        }

        [Theory]
        [MemberData(nameof(BinMessages))]
        public void DeserializesMessageCorrectly_Messages(byte[] messageBin, params Message[] expectedMessages)
        {
            // Arrange

            List<byte> expectedMessage = new List<byte>();
            expectedMessage.Add(0x81);
            expectedMessage.AddRange(SerializeString("messages"));
            expectedMessage.AddRange(messageBin);

            // Act
            ProtocolMessage target = MsgPackHelper.Deserialise<ProtocolMessage>(expectedMessage.ToArray());

            // Assert
            target.Should().NotBeNull();
            target.Messages.Should().NotBeNull();
            Assert.Equal<int>(expectedMessages.Length, target.Messages.Length);
            for (int i = 0; i < expectedMessages.Length; i++)
            {
                Assert.Equal(expectedMessages[i].Name, target.Messages[i].Name);
                Assert.Equal(expectedMessages[i].Data, target.Messages[i].Data);
            }
        }

        /// <summary>
        /// Builds the expected MessagePack byte array for a ProtocolMessage with all 15 fields.
        /// Allows customization of specific fields by passing actual values.
        /// </summary>
        /// <param name="action">Optional action value (null by default)</param>
        /// <param name="channel">Optional channel value (null by default)</param>
        /// <param name="msgSerial">Optional msgSerial value (null by default)</param>
        /// <param name="messages">Optional messages array (null by default)</param>
        /// <param name="presence">Optional presence array (null by default)</param>
        /// <returns>List of bytes representing the expected MessagePack structure</returns>
        private static List<byte> BuildExpectedProtocolMessage(
            ProtocolMessage.MessageAction? action = null,
            string channel = null,
            long? msgSerial = null,
            Message[] messages = null,
            PresenceMessage[] presence = null)
        {
            List<byte> expectedMessage = new List<byte>();
            // MessagePack now serializes all 15 fields (including nulls) in declaration order
            expectedMessage.Add(0x8F); // map with 15 elements

            // Fields in declaration order: params, action, auth, flags, count, error, id, channel,
            // channelSerial, connectionId, msgSerial, timestamp, messages, presence, connectionDetails
            expectedMessage.AddRange(SerializeString("params"));
            expectedMessage.Add(0xc0); // null

            expectedMessage.AddRange(SerializeString("action"));
            expectedMessage.Add(action.HasValue ? (byte)action.Value : (byte)0);

            expectedMessage.AddRange(SerializeString("auth"));
            expectedMessage.Add(0xc0); // null
            expectedMessage.AddRange(SerializeString("flags"));
            expectedMessage.Add(0xc0); // null
            expectedMessage.AddRange(SerializeString("count"));
            expectedMessage.Add(0xc0); // null
            expectedMessage.AddRange(SerializeString("error"));
            expectedMessage.Add(0xc0); // null
            expectedMessage.AddRange(SerializeString("id"));
            expectedMessage.Add(0xc0); // null

            expectedMessage.AddRange(SerializeString("channel"));
            if (channel == null)
            {
                expectedMessage.Add(0xc0); // null
            }
            else
            {
                // Empty string is serialized as empty string, not null
                expectedMessage.AddRange(SerializeString(channel));
            }

            expectedMessage.AddRange(SerializeString("channelSerial"));
            expectedMessage.Add(0xc0); // null
            expectedMessage.AddRange(SerializeString("connectionId"));
            expectedMessage.Add(0xc0); // null

            expectedMessage.AddRange(SerializeString("msgSerial"));
            SerializeMsgSerial(expectedMessage, msgSerial ?? 0);

            expectedMessage.AddRange(SerializeString("timestamp"));
            expectedMessage.Add(0xc0); // null

            expectedMessage.AddRange(SerializeString("messages"));
            SerializeMessages(expectedMessage, messages);

            expectedMessage.AddRange(SerializeString("presence"));
            SerializePresence(expectedMessage, presence);

            expectedMessage.AddRange(SerializeString("connectionDetails"));
            expectedMessage.Add(0xc0); // null

            return expectedMessage;
        }

        private static void SerializeMsgSerial(List<byte> bytes, long msgSerial)
        {
            if (msgSerial >= 0 && msgSerial <= 127)
            {
                // Positive fixint (0x00 to 0x7f)
                bytes.Add((byte)msgSerial);
            }
            else if (msgSerial < 0 && msgSerial >= -32)
            {
                // Negative fixint (0xe0 to 0xff)
                bytes.Add((byte)msgSerial);
            }
            else if (msgSerial >= 0 && msgSerial <= 255)
            {
                // uint8 (0xcc)
                bytes.Add(0xcc);
                bytes.Add((byte)msgSerial);
            }
            else if (msgSerial >= 0 && msgSerial <= 65535)
            {
                // uint16 (0xcd) - MessagePack uses unsigned for positive values
                bytes.Add(0xcd);
                bytes.AddRange(BitConverter.GetBytes((ushort)msgSerial).Reverse());
            }
            else if (msgSerial < 0 && msgSerial >= -128)
            {
                // int8 (0xd0)
                bytes.Add(0xd0);
                bytes.Add((byte)msgSerial);
            }
            else if (msgSerial < 0 && msgSerial >= -32768)
            {
                // int16 (0xd1)
                bytes.Add(0xd1);
                bytes.AddRange(BitConverter.GetBytes((short)msgSerial).Reverse());
            }
            else
            {
                // int64 (0xd3) for larger values
                bytes.Add(0xd3);
                bytes.AddRange(BitConverter.GetBytes(msgSerial).Reverse());
            }
        }

        private static void SerializeMessages(List<byte> bytes, Message[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                bytes.Add(0x90); // empty array
                return;
            }

            // Messages are serialized with all fields including nulls, but empty messages are NOT filtered by OnSerializing
            // The actual serialization includes ALL messages, even those with empty names
            bytes.Add((byte)((0x09 << 4) + messages.Length));
            foreach (Message msg in messages)
            {
                // Each message now has 9 fields in declaration order: id, clientId, connectionId, connectionKey, name, timestamp, data, extras, encoding
                bytes.Add(0x89); // map with 9 elements
                bytes.AddRange(SerializeString("id"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("clientId"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("connectionId"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("connectionKey"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("name"));
                if (string.IsNullOrEmpty(msg.Name))
                {
                    bytes.Add(0xc0); // null for empty name
                }
                else
                {
                    bytes.AddRange(SerializeString(msg.Name));
                }

                bytes.AddRange(SerializeString("timestamp"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("data"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("extras"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("encoding"));
                bytes.Add(0xc0); // null
            }
        }

        private static void SerializePresence(List<byte> bytes, PresenceMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                bytes.Add(0x90); // empty array
                return;
            }

            bytes.Add((byte)((0x09 << 4) + messages.Length));
            foreach (PresenceMessage msg in messages)
            {
                // Each presence message now has 8 fields in declaration order: id, action, clientId, connectionId, connectionKey, data, encoding, timestamp
                bytes.Add(0x88); // map with 8 elements
                bytes.AddRange(SerializeString("id"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("action"));
                bytes.Add((byte)msg.Action);
                bytes.AddRange(SerializeString("clientId"));
                if (!string.IsNullOrEmpty(msg.ClientId))
                {
                    bytes.AddRange(SerializeString(msg.ClientId));
                }
                else
                {
                    bytes.Add(0xc0); // null
                }

                bytes.AddRange(SerializeString("connectionId"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("connectionKey"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("data"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("encoding"));
                bytes.Add(0xc0); // null
                bytes.AddRange(SerializeString("timestamp"));
                bytes.Add(0xc0); // null
            }
        }

        private static byte[] SerializeString(string str)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)(0xa0 + str.Length));
            bytes.AddRange(System.Text.Encoding.GetEncoding("utf-8").GetBytes(str));
            return bytes.ToArray();
        }

        /// <summary>
        /// Test fixture class for msgpack test data.
        /// </summary>
        public class MsgpackTestFixture
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("data")]
            public object Data { get; set; }

            [JsonProperty("encoding")]
            public string Encoding { get; set; }

            [JsonProperty("numRepeat")]
            public int NumRepeat { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("msgpack")]
            public string MsgPack { get; set; }
        }

        /// <summary>
        /// Loads msgpack test fixtures from the embedded resource.
        /// </summary>
        private static List<MsgpackTestFixture> LoadMsgpackFixtures()
        {
            var json = ResourceHelper.GetResource("msgpack_test_fixtures.json");
            return JsonConvert.DeserializeObject<List<MsgpackTestFixture>>(json);
        }

        /// <summary>
        /// Provides test data for msgpack decoding tests.
        /// Each test case is named after the fixture name for better test reporting.
        /// </summary>
        public static IEnumerable<object[]> MsgpackDecodingFixtures
        {
            get
            {
                var fixtures = LoadMsgpackFixtures();
                foreach (var fixture in fixtures)
                {
                    // Pass fixture.Name as first parameter for better test display names
                    yield return new object[] { fixture.Name, fixture };
                }
            }
        }

        [Theory]
        [Trait("spec", "RSL6a3")]
        [MemberData(nameof(MsgpackDecodingFixtures))]
        public void TestMsgpackDecoding(string testName, MsgpackTestFixture fixture)
        {
            Output.WriteLine($"Testing: {testName}");

            // Decode base64 msgpack data
            var msgpackData = Convert.FromBase64String(fixture.MsgPack);

            // Deserialize to ProtocolMessage
            var protoMsg = MsgPackHelper.Deserialise<ProtocolMessage>(msgpackData);
            protoMsg.Should().NotBeNull();
            protoMsg.Messages.Should().NotBeNull();
            protoMsg.Messages.Should().HaveCount(1);

            var msg = protoMsg.Messages[0];

            // Decode the message data using FromEncoded
            var decodedMsg = Message.FromEncoded(msg);

            // Verify decoded data based on type
            switch (fixture.Type)
            {
                case "string":
                    decodedMsg.Data.Should().BeOfType<string>();
                    var expectedString = string.Concat(Enumerable.Repeat(fixture.Data.ToString(), fixture.NumRepeat));
                    decodedMsg.Data.Should().Be(expectedString);
                    (decodedMsg.Data as string).Length.Should().Be(fixture.NumRepeat);
                    break;

                case "binary":
                    decodedMsg.Data.Should().BeOfType<byte[]>();
                    var expectedBytes = System.Text.Encoding.UTF8.GetBytes(
                        string.Concat(Enumerable.Repeat(fixture.Data.ToString(), fixture.NumRepeat)));
                    (decodedMsg.Data as byte[]).Should().Equal(expectedBytes);
                    (decodedMsg.Data as byte[]).Length.Should().Be(fixture.NumRepeat);
                    break;

                case "jsonObject":
                    // For JSON objects, compare as JToken for proper equality
                    var expectedJson = JsonConvert.SerializeObject(fixture.Data);
                    var actualJson = JsonConvert.SerializeObject(decodedMsg.Data);
                    JAssert.DeepEquals(JToken.Parse(expectedJson), JToken.Parse(actualJson), Output).Should().BeTrue();
                    break;

                case "jsonArray":
                    // For JSON arrays, compare as JToken for proper equality
                    var expectedArrayJson = JsonConvert.SerializeObject(fixture.Data);
                    var actualArrayJson = JsonConvert.SerializeObject(decodedMsg.Data);
                    JAssert.DeepEquals(JToken.Parse(expectedArrayJson), JToken.Parse(actualArrayJson), Output).Should().BeTrue();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown fixture type: {fixture.Type}");
            }

            // TODO: Re-encode the message and verify it matches the original
            // Create a new message with the decoded data and encode it
            // Similar to `TestMsgpackDecoding` test in `proto_message_decoding_test.go`
            // This will need omitting keys for null values, current serializer omits keys with nulls.
        }

        public MsgPackMessageSerializerTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
