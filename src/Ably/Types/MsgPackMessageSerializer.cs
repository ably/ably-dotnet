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
                long fields;
                unpacker.ReadMapLength(out fields);
                string reason = "";
                int statusCode = 0, code = 0;
                string fieldName;
                for (int i = 0; i < fields; i++)
                {
                    unpacker.ReadString(out fieldName);
                    switch (fieldName)
                    {
                        case "message" :
                            unpacker.ReadString(out reason);
                            break;
                        case "statusCode":
                            unpacker.ReadInt32(out statusCode);
                            break;
                        case "code":
                            unpacker.ReadInt32(out code);
                            break;
                    }
                }
                message.Error = new ErrorInfo(reason, code, statusCode == 0 ? null : (System.Net.HttpStatusCode?)statusCode);
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
                message.Timestamp = result.FromUnixTimeInMilliseconds();
            });
            unpackActions.Add("messages", (unpacker, message) =>
            {
                long arrayLength;
                unpacker.ReadArrayLength(out arrayLength);
                message.Messages = new Message[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                {
                    message.Messages[i] = DeserializeMessage(unpacker);
                }
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

                    if (message.Messages != null)
                    {
                        packer.PackString("messages");
                        packer.PackArrayHeader(message.Messages.Length);
                        foreach (Message msg in message.Messages)
                        {
                            SerializeMessage(msg, packer);
                        }
                    }
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

        private static void SerializeMessage(Message message, Packer packer)
        {
            int fieldCount = 0;
            if (!string.IsNullOrEmpty(message.Name)) fieldCount++;
            if (message.Data != null) fieldCount++;

            packer.PackMapHeader(fieldCount);

            if (!string.IsNullOrEmpty(message.Name))
            {
                packer.PackString("name");
                packer.PackString(message.Name);
            }
            if (message.Data != null)
            {
                packer.PackString("data");
                if (message.Data is byte[])
                {
                    packer.PackRaw(message.Data as byte[]);
                }
                else
                {
                    packer.PackString(message.Data.ToString());
                }
            }
        }

        private static Message DeserializeMessage(Unpacker unpacker)
        {
            Message message = new Message();

            long fields;
            unpacker.ReadMapLength(out fields);
            string fieldName;
            for (int i = 0; i < fields; i++)
            {
                unpacker.ReadString(out fieldName);
                switch (fieldName)
                {
                    case "name":
                        {
                            string result;
                            unpacker.ReadString(out result);
                            message.Name = result;
                        }
                        break;
                    case "timestamp":
                        {
                            long result;
                            unpacker.ReadInt64(out result);
                            message.Timestamp = result.FromUnixTimeInMilliseconds();
                        }
                        break;
                    case "data":
                        {
                            MessagePackObject result;
                            unpacker.ReadObject(out result);
                            if (result.IsTypeOf<string>().GetValueOrDefault(false))
                            {
                                message.Data = result.AsStringUtf8();
                            }
                        }
                        break;
                }
            }

            return message;
        }
    }
}
