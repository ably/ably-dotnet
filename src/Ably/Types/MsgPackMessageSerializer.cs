using System;
using MsgPack;
using System.IO;

namespace Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        private static System.Collections.Generic.Dictionary<string, Action<Unpacker, ProtocolMessage>> unpackActions;

        static MsgPackMessageSerializer()
        {
            unpackActions = new System.Collections.Generic.Dictionary<string, Action<Unpacker, ProtocolMessage>>();
            unpackActions.Add("action", (unpacker, message) =>
            {
                int result;
                unpacker.ReadInt32(out result);
                message.Action = (ProtocolMessage.MessageAction)result;
            });
            unpackActions.Add("flags", (unpacker, message) =>
            {
                int result;
                unpacker.ReadInt32(out result);
                message.Flags = (ProtocolMessage.MessageFlag)result;
            });
            unpackActions.Add("count", (unpacker, message) =>
            {
                int result;
                unpacker.ReadInt32(out result);
                message.Count = result;
            });
            unpackActions.Add("msgSerial", (unpacker, message) =>
            {
                long result;
                unpacker.ReadInt64(out result);
                message.MsgSerial = result;
            });
            unpackActions.Add("error", (unpacker, message) =>
            {
            });
            unpackActions.Add("id", (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.Id = result;
            });
            unpackActions.Add("channel", (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.Channel = result;
            });
            unpackActions.Add("channelSerial", (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.ChannelSerial = result;
            });
            unpackActions.Add("connectionId", (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.ConnectionId = result;
            });
            unpackActions.Add("connectionKey", (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.ConnectionKey = result;
            });
            unpackActions.Add("connectionSerial", (unpacker, message) =>
            {
                long result;
                unpacker.ReadInt64(out result);
                message.ConnectionSerial = result;
            });
            unpackActions.Add("timestamp", (unpacker, message) =>
            {
                long result;
                unpacker.ReadInt64(out result);
                message.Timestamp = result;
            });
            unpackActions.Add("messages", (unpacker, message) =>
            {
            });
            unpackActions.Add("presence", (unpacker, message) =>
            {
            });
        }

        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            byte[] result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (Packer packer = Packer.Create(stream))
                {
                    int fieldCount = 2; //action & msgSerial
                    if (!string.IsNullOrEmpty(message.Channel)) fieldCount++;
                    if (message.Messages != null) fieldCount++;

                    // serialize message
                    packer.PackMapHeader(fieldCount);

                    packer.PackString("action");
                    packer.Pack<int>((int)message.Action);

                    if (!string.IsNullOrEmpty(message.Channel))
                    {
                        packer.PackString("channel");
                        packer.PackString(message.Channel);
                    }

                    packer.PackString("msgSerial");
                    packer.Pack<long>(message.MsgSerial);
                }
                result = stream.ToArray();
            }
            return result;
        }

        public ProtocolMessage DeserializeProtocolMessage(object value)
        {
            ProtocolMessage message = new ProtocolMessage();
            using (MemoryStream stream = new MemoryStream((byte[])value))
            {
                using (Unpacker unpacker = Unpacker.Create(stream))
                {
                    long fieldCount = 0;
                    unpacker.ReadMapLength(out fieldCount);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        string fieldName;
                        unpacker.ReadString(out fieldName);
                        unpackActions[fieldName](unpacker, message);
                    }
                }
            }
            return message;
        }
    }
}
