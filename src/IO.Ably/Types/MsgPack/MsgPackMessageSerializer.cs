using System;
using System.Linq;
using System.Net;
using IO.Ably.Types.MsgPack;

namespace IO.Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        private static readonly TypeMetadata meta;

        static MsgPackMessageSerializer()
        {
            meta = new TypeMetadata(typeof (ProtocolMessage));

            var mdMessage = new TypeMetadata(typeof (Message));

            mdMessage.remove("data");
            mdMessage.add("data");
            mdMessage.setCustom("data",
                (obj, packer) =>
                {
                    var data = ((Message) obj).data;
                    if (data is byte[])
                        packer.PackRaw(data as byte[]);
                    else
                        packer.PackString(data.ToString());
                },
                (unpacker, obj) =>
                {
                    var result = unpacker.ReadItemData();
                    ((Message) obj).data = result.unpack();
                });

            var mdPresence = new TypeMetadata(typeof (PresenceMessage));

            meta.setCustom("messages",
                (obj, packer) =>
                {
                    var arr = ((ProtocolMessage) obj).messages.Where(m => !m.IsEmpty()).ToArray();
                    packer.packArray(mdMessage, arr);
                },
                (unp, obj) =>
                {
                    var arr = unp.unpackArray<Message>(mdMessage);
                    ((ProtocolMessage) obj).messages = arr;
                });

            meta.setCustom("presence",
                (obj, packer) =>
                {
                    var arr = ((ProtocolMessage) obj).presence;
                    packer.packArray(mdPresence, arr);
                },
                (unp, obj) =>
                {
                    var arr = unp.unpackArray<PresenceMessage>(mdPresence);
                    ((ProtocolMessage) obj).presence = arr;
                });

            meta.setCustom("flags",
                (obj, packer) => { throw new NotSupportedException(); },
                (unp, obj) =>
                {
                    int i;
                    unp.ReadInt32(out i);
                    var flags = (ProtocolMessage.MessageFlag) (byte) i;
                    ((ProtocolMessage) obj).flags = flags;
                });

            meta.setCustom("timestamp",
                (obj, packer) =>
                {
                    var dto = ((ProtocolMessage) obj).timestamp.Value;
                    packer.Pack(dto.ToUnixTimeInMilliseconds());
                },
                (unp, obj) =>
                {
                    long ms;
                    unp.ReadInt64(out ms);
                    ((ProtocolMessage) obj).timestamp = ms.FromUnixTimeInMilliseconds();
                });

            var mdErrorInfo = new TypeMetadata(typeof (ErrorInfo));
            mdErrorInfo.setCustom("statusCode",
                (obj, packer) =>
                {
                    var code = ((ErrorInfo) obj).statusCode.Value;
                    var iCode = (int) code;
                    mdErrorInfo.serialize(iCode);
                },
                (unp, obj) =>
                {
                    int iCode;
                    unp.ReadInt32(out iCode);
                    var code = (HttpStatusCode) iCode;
                    ((ErrorInfo) obj).statusCode = code;
                });

            meta.setCustom("error",
                (obj, packer) => { mdErrorInfo.serialize(((ProtocolMessage) obj).error, packer); },
                (unp, obj) => { ((ProtocolMessage) obj).error = (ErrorInfo) mdErrorInfo.deserialize(unp); });

            var mdConnectionDetails = new TypeMetadata(typeof (ConnectionDetailsMessage));
            meta.setCustom("connectionDetails",
                (obj, packer) => { mdConnectionDetails.serialize(((ProtocolMessage) obj).connectionDetails, packer); },
                (unp, obj) =>
                {
                    ((ProtocolMessage) obj).connectionDetails =
                        (ConnectionDetailsMessage) mdConnectionDetails.deserialize(unp);
                });
        }

        public ProtocolMessage DeserializeProtocolMessage(object value)
        {
            return (ProtocolMessage) meta.deserialize((byte[]) value);
        }

        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            return meta.serialize(message);
        }
    }
}