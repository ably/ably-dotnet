using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Ably.Rest;
using MsgPack;
using Newtonsoft.Json;

namespace Ably.MessageEncoders
{
    internal class MessageHandler : IMessageHandler
    {
        private readonly Protocol _protocol;
        public List<MessageEncoder> Encoders = new List<MessageEncoder>();

        public MessageHandler()
            : this(Protocol.MsgPack)
        {

        }

        public MessageHandler(Protocol protocol)
        {
            _protocol = protocol;

            InitialiseMessageEncoders(protocol);
        }

        private void InitialiseMessageEncoders(Protocol protocol)
        {
            Encoders.Add(new JsonEncoder(protocol));
            Encoders.Add(new Utf8Encoder(protocol));
            Encoders.Add(new CipherEncoder(protocol));
            Encoders.Add(new Base64Encoder(protocol));

            Logger.Debug(string.Format("Initialising message encodings. {0} initialised", string.Join(",", Encoders.Select( x=> x.EncodingName))));
        }

        public T ParseMessagesResponse<T>(AblyResponse response) where T : class
        {
            if (response.Type == ResponseType.Json)
                return JsonConvert.DeserializeObject<T>(response.TextResponse);
            return default(T);
        }

        public IEnumerable<PresenceMessage> ParsePresenceMessages(AblyResponse response)
        {
            if (response.Type == ResponseType.Json)
            {
                var messages = JsonConvert.DeserializeObject<List<PresenceMessage>>(response.TextResponse);
                ProcessMessages(messages, new ChannelOptions());
                return messages;
            }

            var payloads = MsgPackHelper.DeSerialise(response.Body, typeof(List<PresenceMessage>)) as List<PresenceMessage>;
            foreach (var payload in payloads.Where(x => x.data != null))
            {
                //Unwrap the data objects because message pack leaves them as a MessagePackObject
                payload.data = ((MessagePackObject)payload.data).ToObject();
            }
            ProcessMessages(payloads, new ChannelOptions());
            return payloads;
        }

        public IEnumerable<Message> ParseMessagesResponse(AblyResponse response, ChannelOptions options)
        {
            Contract.Assert(options != null);

            if (response.Type == ResponseType.Json)
            {
                var messages = JsonConvert.DeserializeObject<List<Message>>(response.TextResponse);
                ProcessMessages(messages, options);
                return messages;
            }

            var payloads = MsgPackHelper.DeSerialise(response.Body, typeof(List<Message>)) as List<Message>;
            foreach (var payload in payloads.Where(x => x.data != null))
            {
                //Unwrap the data objects because message pack leaves them as a MessagePackObject
                payload.data = ((MessagePackObject)payload.data).ToObject();
            }
            ProcessMessages(payloads, options);
            return payloads;
        }

        private void ProcessMessages<T>(IEnumerable<T> payloads, ChannelOptions options) where T : IEncodedMessage
        {
            DecodePayloads(options, payloads as IEnumerable<IEncodedMessage>);
        }

        public void SetRequestBody(AblyRequest request)
        {
            request.RequestBody = GetRequestBody(request);
        }

        public byte[] GetRequestBody(AblyRequest request)
        {
            Logger.Debug("Encoding request body.");
            if (request.PostData == null)
                return new byte[] { };

            if (request.PostData is IEnumerable<Message>)
                return GetMessagesRequestBody(request.PostData as IEnumerable<Message>,
                    request.ChannelOptions);

            Logger.Debug(string.Format("Payload: {0}", JsonConvert.SerializeObject(request.PostData)));

            if (_protocol == Protocol.Json)
                return JsonConvert.SerializeObject(request.PostData).GetBytes();
            return MsgPackHelper.Serialise(request.PostData);
        }

        private byte[] GetMessagesRequestBody(IEnumerable<Message> payloads, ChannelOptions options)
        {
            EncodePayloads(options, payloads);

            if (_protocol == Protocol.MsgPack)
            {
                return MsgPackHelper.Serialise(payloads);
            }
            return JsonConvert.SerializeObject(payloads).GetBytes();
        }

        internal void EncodePayloads(ChannelOptions options, IEnumerable<IEncodedMessage> payloads)
        {
            foreach (var payload in payloads)
                EncodePayload(payload, options);
        }

        internal void DecodePayloads(ChannelOptions options, IEnumerable<IEncodedMessage> payloads)
        {
            foreach (var payload in payloads)
                DecodePayload(payload, options);
        }

        private void EncodePayload(IEncodedMessage payload, ChannelOptions options)
        {
            foreach (var encoder in Encoders)
            {
                encoder.Encode(payload, options);
            }
        }

        private void DecodePayload(IEncodedMessage payload, ChannelOptions options)
        {
            foreach (var encoder in (Encoders as IEnumerable<MessageEncoder>).Reverse())
            {
                encoder.Decode(payload, options);
            }
        }

        public T ParseResponse<T>(AblyRequest request, AblyResponse response) where T : class
        {
            LogResponse(response);
            if (typeof(T) == typeof(PaginatedResource<Message>))
            {
                var result = PaginatedResource.InitialisePartialResult<Message>(response.Headers, GetLimit(request));
                result.AddRange(ParseMessagesResponse(response, request.ChannelOptions));
                return result as T;
            }

            if (typeof(T) == typeof(PaginatedResource<Stats>))
            {
                var result = PaginatedResource.InitialisePartialResult<Stats>(response.Headers, GetLimit(request));
                result.AddRange(ParseStatsResponse(response));
                return result as T;
            }

            if (typeof(T) == typeof(PaginatedResource<PresenceMessage>))
            {
                var result = PaginatedResource.InitialisePartialResult<PresenceMessage>(response.Headers, GetLimit(request));
                result.AddRange(ParsePresenceMessages(response));
                return result as T;
            }

            var responseText = response.TextResponse;
            if (_protocol == Protocol.MsgPack)
            {
                //A bit of a hack. Message pack serialiser does not like capability objects
                responseText = MsgPackHelper.DeSerialise(response.Body, typeof (MessagePackObject)).ToString();
            }

            return (T)JsonConvert.DeserializeObject(responseText, typeof(T));
        }

        private void LogResponse(AblyResponse response)
        {
            Logger.Info("Protocol:" + _protocol);
            try
            {
                var responseBody = response.TextResponse;
                if (_protocol == Protocol.MsgPack && response.Body != null)
                {
                    responseBody = MsgPackHelper.DeSerialise(response.Body, typeof(MessagePackObject)).ToString();
                }
                Logger.Debug("Response: " + responseBody);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while logging response body.", ex);
            }

        }

        private IEnumerable<Stats> ParseStatsResponse(AblyResponse response)
        {
            var body = response.TextResponse;
            if (_protocol == Protocol.MsgPack)
            {
                body = ((MessagePackObject)MsgPackHelper.DeSerialise(response.Body, typeof (MessagePackObject))).ToString();
            }
            return JsonConvert.DeserializeObject<List<Stats>>(body);
        }

        private static int GetLimit(AblyRequest request)
        {
            if (request.QueryParameters.ContainsKey("limit"))
            {
                var limitQuery = request.QueryParameters["limit"];
                var limit = Config.Limit;
                if (limitQuery.IsNotEmpty())
                    limit = int.Parse(limitQuery);
                return limit;
            }
            return Config.Limit;
        }
    }

}