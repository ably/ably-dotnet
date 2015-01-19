using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Ably.MessageEncoders;

namespace Ably
{
    internal class MessageHandler
    {
        private readonly Protocol _protocol;
        public static List<MessageEncoder> Encoders = new List<MessageEncoder>();

        public MessageHandler() : this(Protocol.MsgPack)
        {
            
        }

        public MessageHandler(Protocol protocol)
        {
            _protocol = protocol;

            InitialiseMessageEncoders(protocol);
        }

        private void InitialiseMessageEncoders(Protocol protocol)
        {
            Encoders.Add(new Utf8Encoder(protocol));
            Encoders.Add(new JsonEncoder(protocol));
            Encoders.Add(new CipherEncoder(protocol));
            Encoders.Add(new Base64Encoder(protocol));
        }

        public void SetRequestBody(AblyRequest request)
        {
            request.RequestBody = GetRequestBody(request);
        }

        public byte[] GetRequestBody(AblyRequest request)
        {
            if (request.PostData == null)
                return new byte[] { };

            if (request.PostData is Message)
                return GetMessagesRequestBody(new[] { request.PostData as Message },
                    request.ChannelOptions);

            if (request.PostData is IEnumerable<Message>)
                return GetMessagesRequestBody(request.PostData as IEnumerable<Message>,
                    request.ChannelOptions);

            //Any other requests like auth or history etc
            return JsonConvert.SerializeObject(request.PostData).GetBytes();
        }

        private byte[] GetMessagesRequestBody(IEnumerable<Message> messages, ChannelOptions options)
        {
            var payloads = messages.Select(MessagePayload.FromMessage).ToList();
            
            EncodePayloads(options, payloads);

            if (_protocol == Protocol.MsgPack)
            {
                var serializer = SerializationContext.Default.GetSerializer<List<MessagePayload>>();
                using (var memoryStream = new MemoryStream())
                {
                    serializer.Pack(memoryStream, payloads);
                    return memoryStream.ToArray();
                }
            }
            return JsonConvert.SerializeObject(payloads).GetBytes();
        }

        internal static void EncodePayloads(ChannelOptions options, List<MessagePayload> payloads)
        {
            foreach (var payload in payloads)
                EncodePayload(payload, options);
        }

        internal static MessagePayload CreateMessagePayload(Message message, AblyRequest request)
        {
            var payload = new MessagePayload()
            {
                name = message.Name,
                timestamp = message.TimeStamp.DateTime.ToUnixTime()
            };

            return payload;
        }

        private static void EncodePayload(MessagePayload payload, ChannelOptions options)
        {
            foreach (var encoder in Encoders)
            {
                encoder.Encode(payload, options);
            }
        }
    }
}