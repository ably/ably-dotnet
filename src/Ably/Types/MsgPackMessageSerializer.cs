using System;
using System.IO;
using System.Linq;
using MsgPack;
using System.Collections.Generic;

namespace Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        private static System.Collections.Generic.Dictionary<string, Action<Unpacker, ProtocolMessage>> unpackActions;
        private static Dictionary<Type, Func<MessagePackObject, object>> resolver;

        static MsgPackMessageSerializer()
        {
            unpackActions = new System.Collections.Generic.Dictionary<string, Action<Unpacker, ProtocolMessage>>();
            unpackActions.Add(ProtocolMessage.ActionPropertyName, (unpacker, message) =>
            {
                int result;
                unpacker.ReadInt32(out result);
                message.Action = (ProtocolMessage.MessageAction)result;
            });
            unpackActions.Add(ProtocolMessage.FlagsPropertyName, (unpacker, message) =>
            {
                int result;
                unpacker.ReadInt32(out result);
                message.Flags = (ProtocolMessage.MessageFlag)result;
            });
            unpackActions.Add(ProtocolMessage.CountPropertyName, (unpacker, message) =>
            {
                int result;
                unpacker.ReadInt32(out result);
                message.Count = result;
            });
            unpackActions.Add(ProtocolMessage.MsgSerialPropertyName, (unpacker, message) =>
            {
                long result;
                unpacker.ReadInt64(out result);
                message.MsgSerial = result;
            });
            unpackActions.Add(ProtocolMessage.ErrorPropertyName, (unpacker, message) =>
            {
                long fields;
                unpacker.ReadMapLength(out fields);
                string reason = string.Empty;
                int statusCode = 0, code = 0;
                string fieldName;
                for (int i = 0; i < fields; i++)
                {
                    unpacker.ReadString(out fieldName);
                    switch (fieldName)
                    {
                        case ErrorInfo.ReasonPropertyName :
                            unpacker.ReadString(out reason);
                            break;
                        case ErrorInfo.StatusCodePropertyName :
                            unpacker.ReadInt32(out statusCode);
                            break;
                        case ErrorInfo.CodePropertyName :
                            unpacker.ReadInt32(out code);
                            break;
                    }
                }
                message.Error = new ErrorInfo(reason, code, statusCode == 0 ? null : (System.Net.HttpStatusCode?)statusCode);
            });
            unpackActions.Add(ProtocolMessage.IdPropertyName, (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.Id = result;
            });
            unpackActions.Add(ProtocolMessage.ChannelPropertyName, (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.Channel = result;
            });
            unpackActions.Add(ProtocolMessage.ChannelSerialPropertyName, (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.ChannelSerial = result;
            });
            unpackActions.Add(ProtocolMessage.ConnectionIdPropertyName, (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.ConnectionId = result;
            });
            unpackActions.Add(ProtocolMessage.ConnectionKeyPropertyName, (unpacker, message) =>
            {
                string result;
                unpacker.ReadString(out result);
                message.ConnectionKey = result;
            });
            unpackActions.Add(ProtocolMessage.ConnectionSerialPropertyName, (unpacker, message) =>
            {
                long result;
                unpacker.ReadInt64(out result);
                message.ConnectionSerial = result;
            });
            unpackActions.Add(ProtocolMessage.TimestampPropertyName, (unpacker, message) =>
            {
                long result;
                unpacker.ReadInt64(out result);
                message.Timestamp = result.FromUnixTimeInMilliseconds();
            });
            unpackActions.Add(ProtocolMessage.MessagesPropertyName, (unpacker, message) =>
            {
                long arrayLength;
                unpacker.ReadArrayLength(out arrayLength);
                message.Messages = new Message[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                {
                    message.Messages[i] = DeserializeMessage(unpacker);
                }
            });
            unpackActions.Add(ProtocolMessage.PresencePropertyName, (unpacker, message) =>
            {
                long arrayLength;
                unpacker.ReadArrayLength(out arrayLength);
                message.Presence = new PresenceMessage[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                {
                    message.Presence[i] = DeserializePresenceMessage(unpacker);
                }
            });

            resolver = new Dictionary<Type, Func<MessagePackObject, object>>();
            resolver.Add(typeof(Byte), r => r.AsByte());
            resolver.Add(typeof(SByte), r => r.AsSByte());
            resolver.Add(typeof(Boolean), r => r.AsBoolean());
            resolver.Add(typeof(UInt16), r => r.AsUInt16());
            resolver.Add(typeof(UInt32), r => r.AsUInt32());
            resolver.Add(typeof(UInt64), r => r.AsUInt64());
            resolver.Add(typeof(Int16), r => r.AsInt16());
            resolver.Add(typeof(Int32), r => r.AsInt32());
            resolver.Add(typeof(Int64), r => r.AsInt64());
            resolver.Add(typeof(Single), r => r.AsSingle());
            resolver.Add(typeof(Double), r => r.AsDouble());
            resolver.Add(typeof(String), r => r.AsStringUtf8());
        }

        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            byte[] result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (Packer packer = Packer.Create(stream))
                {
                    int fieldCount = GetFieldCount(message);

                    // serialize message
                    packer.PackMapHeader(fieldCount);

                    packer.PackString(ProtocolMessage.ActionPropertyName);
                    packer.Pack<int>((int)message.Action);

                    if (!string.IsNullOrEmpty(message.Channel))
                    {
                        packer.PackString(ProtocolMessage.ChannelPropertyName);
                        packer.PackString(message.Channel);
                    }

                    packer.PackString(ProtocolMessage.MsgSerialPropertyName);
                    packer.Pack<long>(message.MsgSerial);

                    if (message.Messages != null)
                    {
                        var validMessages = message.Messages.Where(c => GetFieldCount(c) > 0);
                        if (validMessages.Any())
                        {
                            packer.PackString(ProtocolMessage.MessagesPropertyName);
                            packer.PackArrayHeader(validMessages.Count());
                            foreach (Message msg in validMessages)
                            {
                                SerializeMessage(msg, packer);
                            }
                        }
                    }

                    if (message.Presence != null)
                    {
                        var validMessages = message.Presence.Where(c => GetFieldCount(c) > 0);
                        if (validMessages.Any())
                        {
                            packer.PackString(ProtocolMessage.PresencePropertyName);
                            packer.PackArrayHeader(validMessages.Count());
                            foreach (PresenceMessage msg in validMessages)
                            {
                                SerializePresenceMessage(msg, packer);
                            }
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

        private static int GetFieldCount(Message message)
        {
            int fieldCount = 0;
            if (!string.IsNullOrEmpty(message.Name)) fieldCount++;
            if (message.Data != null) fieldCount++;
            return fieldCount;
        }

        private static int GetFieldCount(ProtocolMessage message)
        {
            int fieldCount = 2; //action & msgSerial
            if (!string.IsNullOrEmpty(message.Channel)) fieldCount++;
            if (message.Messages != null && message.Messages.Any(c => GetFieldCount(c) > 0)) fieldCount++;
            if (message.Presence != null) fieldCount++;
            return fieldCount;
        }

        private static int GetFieldCount(PresenceMessage message)
        {
            int fieldCount = 1; //action
            if (!string.IsNullOrEmpty(message.ClientId)) fieldCount++;
            if (!string.IsNullOrEmpty(message.ConnectionId)) fieldCount++;
            if (message.Data != null) fieldCount++;
            if (!string.IsNullOrEmpty(message.Encoding)) fieldCount++;
            if (!string.IsNullOrEmpty(message.Id)) fieldCount++;
            if (message.Timestamp.Ticks > 0) fieldCount++;
            return fieldCount;
        }

        private static void SerializeMessage(Message message, Packer packer)
        {
            int fieldCount = GetFieldCount(message);
            packer.PackMapHeader(fieldCount);

            if (!string.IsNullOrEmpty(message.Name))
            {
                packer.PackString(Message.NamePropertyName);
                packer.PackString(message.Name);
            }
            if (message.Data != null)
            {
                packer.PackString(Message.DataPropertyName);
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

        private static void SerializePresenceMessage(PresenceMessage message, Packer packer)
        {
            int fieldCount = GetFieldCount(message);
            packer.PackMapHeader(fieldCount);

            packer.PackString(PresenceMessage.ActionPropertyName);
            packer.Pack<int>((int)message.Action);

            if (!string.IsNullOrEmpty(message.Id))
            {
                packer.PackString(PresenceMessage.IdPropertyName);
                packer.Pack(message.Id);
            }
            if (!string.IsNullOrEmpty(message.ClientId))
            {
                packer.PackString(PresenceMessage.ClientIdPropertyName);
                packer.Pack(message.ClientId);
            }
            if (!string.IsNullOrEmpty(message.ConnectionId))
            {
                packer.PackString(PresenceMessage.ConnectionIdPropertyName);
                packer.Pack(message.ConnectionId);
            }
            if (message.Timestamp.Ticks > 0)
            {
                packer.PackString(PresenceMessage.TimestampPropertyName);
                packer.Pack(message.Timestamp.ToUnixTimeInMilliseconds());
            }
            if (message.Data != null)
            {
                packer.PackString(PresenceMessage.DataPropertyName);
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
                    case Message.NamePropertyName:
                        {
                            string result;
                            unpacker.ReadString(out result);
                            message.Name = result;
                        }
                        break;
                    case Message.TimestampPropertyName:
                        {
                            long result;
                            unpacker.ReadInt64(out result);
                            message.Timestamp = result.FromUnixTimeInMilliseconds();
                        }
                        break;
                    case Message.DataPropertyName:
                        {
                            MessagePackObject result = unpacker.ReadItemData();
                            message.Data = ParseResult(result);
                        }
                        break;
                }
            }

            return message;
        }

        private static PresenceMessage DeserializePresenceMessage(Unpacker unpacker)
        {
            PresenceMessage message = new PresenceMessage();

            long fields;
            unpacker.ReadMapLength(out fields);
            string fieldName;
            for (int i = 0; i < fields; i++)
            {
                unpacker.ReadString(out fieldName);
                switch (fieldName)
                {
                    case PresenceMessage.ActionPropertyName :
                        {
                            int result;
                            unpacker.ReadInt32(out result);
                            message.Action = (PresenceMessage.ActionType)result;
                        }
                        break;
                    case PresenceMessage.IdPropertyName :
                        {
                            string result;
                            unpacker.ReadString(out result);
                            message.Id = result;
                        }
                        break;
                    case PresenceMessage.ClientIdPropertyName :
                        {
                            string result;
                            unpacker.ReadString(out result);
                            message.ClientId = result;
                        }
                        break;
                    case PresenceMessage.ConnectionIdPropertyName :
                        {
                            string result;
                            unpacker.ReadString(out result);
                            message.ConnectionId = result;
                        }
                        break;
                    case PresenceMessage.TimestampPropertyName :
                        {
                            long result;
                            unpacker.ReadInt64(out result);
                            message.Timestamp = result.FromUnixTimeInMilliseconds();
                        }
                        break;
                    case PresenceMessage.DataPropertyName :
                        {
                            MessagePackObject result = unpacker.ReadItemData();
                            message.Data = ParseResult(result);
                        }
                        break;
                }
            }

            return message;
        }

        private static object ParseResult(MessagePackObject obj)
        {
            if (obj.IsList)
            {
                List<object> data = new List<object>();
                foreach (MessagePackObject objItem in obj.AsList())
                {
                    data.Add(ParseResult(objItem));
                }
                return data.ToArray();
            }
            else if (obj.IsMap)
            {
#if SILVERLIGHT
                var data = new System.Collections.Generic.Dictionary<object, object>();
#else
                System.Collections.Hashtable data = new System.Collections.Hashtable();
#endif
                foreach (var objItem in obj.AsDictionary())
                {
                    data.Add(ParseResult(objItem.Key), ParseResult(objItem.Value));
                }
                return data;
            }
            else
            {
                if (obj.UnderlyingType != null && resolver.ContainsKey(obj.UnderlyingType))
                {
                    return resolver[obj.UnderlyingType](obj);
                }
            }
            return null;
        }
    }
}
