using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.MessageEncoders
{
    internal class MessageHandler
    {
        internal ILogger Logger { get; private set; }

        private static readonly Type[] UnsupportedTypes = new[]
            {
                typeof(short), typeof(int), typeof(double), typeof(float), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(byte), typeof(bool),
                typeof(long), typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte)
            };

        private readonly Protocol _protocol;
        public readonly List<MessageEncoder> Encoders = new List<MessageEncoder>();

        public MessageHandler()
            : this(IO.Ably.DefaultLogger.LoggerInstance, Defaults.Protocol) { }

        public MessageHandler(Protocol protocol)
            : this(IO.Ably.DefaultLogger.LoggerInstance, protocol) { }

        public MessageHandler(ILogger logger, Protocol protocol)
        {
            Logger = logger;
            _protocol = protocol;

            InitializeMessageEncoders(protocol);
        }

        private void InitializeMessageEncoders(Protocol protocol)
        {
            Encoders.Add(new JsonEncoder(protocol));
            Encoders.Add(new Utf8Encoder(protocol));
            Encoders.Add(new CipherEncoder(protocol));
            Encoders.Add(new Base64Encoder(protocol));

            Logger.Debug(
                $"Initializing message encodings. {string.Join(",", Encoders.Select(x => x.EncodingName))} initialized");
        }

        public IEnumerable<PresenceMessage> ParsePresenceMessages(AblyResponse response, ChannelOptions options)
        {
            if (response.Type == ResponseType.Json)
            {
                var messages = JsonHelper.Deserialize<List<PresenceMessage>>(response.TextResponse);
                ProcessMessages(messages, options);
                return messages;
            }

#if MSGPACK
            var payloads = MsgPackHelper.Deserialise(response.Body, typeof(List<PresenceMessage>)) as List<PresenceMessage>;
            ProcessMessages(payloads, options);
            return payloads;
#else
            throw new AblyException($"Response of type '{response.Type}' is invalid because MsgPack support was not enabled for this build.");

#endif
        }

        public IEnumerable<Message> ParseMessagesResponse(AblyResponse response, ChannelOptions options)
        {
            if (response.Type == ResponseType.Json)
            {
                var messages = JsonHelper.Deserialize<List<Message>>(response.TextResponse);
                ProcessMessages(messages, options);
                return messages;
            }

#if MSGPACK
            var payloads = MsgPackHelper.Deserialise(response.Body, typeof(List<Message>)) as List<Message>;
            ProcessMessages(payloads, options);
            return payloads;
#else
            throw new AblyException($"Response of type '{response.Type}' is invalid because MsgPack support was not enabled for this build.");

#endif
        }

        private void ProcessMessages<T>(IEnumerable<T> payloads, ChannelOptions options) where T : IMessage
        {
            DecodePayloads(options, payloads as IEnumerable<IMessage>);
        }

        public void SetRequestBody(AblyRequest request)
        {
            request.RequestBody = GetRequestBody(request);
#if MSGPACK
            if (_protocol == Protocol.MsgPack && Logger.IsDebug)
            {
                LogRequestBody(request.RequestBody);
            }
#endif
        }

        private void LogRequestBody(byte[] requestBody)
        {
            try
            {
#if MSGPACK
                var body = MsgPackHelper.DeserialiseMsgPackObject(requestBody)?.ToString();
                Logger.Debug("RequestBody: " + (body ?? "No body present"));
#else
                Logger.Debug("RequestBody: MsgPack disabled, cannot log request");
#endif
            }
            catch (Exception ex)
            {
                Logger.Error("Error while logging request body.", ex);
            }
        }

        public byte[] GetRequestBody(AblyRequest request)
        {
            if (request.PostData == null)
            {
                return new byte[] { };
            }

            if (request.PostData is IEnumerable<Message>)
            {
                return GetMessagesRequestBody(
                    request.PostData as IEnumerable<Message>,
                    request.ChannelOptions);
            }

            byte[] result;
            if (_protocol == Protocol.Json || !Config.MsgPackEnabled)
            {
                result = JsonHelper.Serialize(request.PostData).GetBytes();
            }
            else
            {
#if MSGPACK
                result = MsgPackHelper.Serialise(request.PostData);
#endif
            }

            if (Logger.IsDebug)
            {
                Logger.Debug("Request body: " + result.GetText());
            }

            return result;
        }

        private byte[] GetMessagesRequestBody(IEnumerable<Message> payloads, ChannelOptions options)
        {
            EncodePayloads(options, payloads);
#if MSGPACK
            if (_protocol == Protocol.MsgPack)
            {
                return MsgPackHelper.Serialise(payloads);
            }
#endif
            return JsonHelper.Serialize(payloads).GetBytes();
        }

        internal Result EncodePayloads(ChannelOptions options, IEnumerable<IMessage> payloads)
        {
            var result = Result.Ok();
            foreach (var payload in payloads)
            {
                result = Result.Combine(result, EncodePayload(payload, options));
            }

            return result;
        }

        internal Result DecodePayloads(ChannelOptions options, IEnumerable<IMessage> payloads)
        {
            var result = Result.Ok();
            foreach (var payload in payloads)
            {
                result = Result.Combine(result, DecodePayload(payload, options));
            }

            return result;
        }

        private Result EncodePayload(IMessage payload, ChannelOptions options)
        {
            ValidatePayloadDataType(payload);
            var result = Result.Ok();
            foreach (var encoder in Encoders)
            {
                result = Result.Combine(result, encoder.Encode(payload, options));
            }

            return result;
        }

        private void ValidatePayloadDataType(IMessage payload)
        {
            if (payload.Data == null)
            {
                return;
            }

            var dataType = payload.Data.GetType();
            var testType = GetNullableType(dataType) ?? dataType;
            if (UnsupportedTypes.Contains(testType))
            {
                throw new AblyException("Unsupported payload type. Only string, binarydata (byte[]) and objects convertable to json are supported being directly sent. This ensures that libraries in different languages work correctly. To send the requested value please create a DTO and pass the DTO as payload. For example if you are sending an '10' then create a class with one property; assign the value to the property and send it.");
            }
        }

        private static Type GetNullableType(Type type)
        {
            if (type.GetTypeInfo().IsValueType == false)
            {
                return null; // ref-type
            }

            return Nullable.GetUnderlyingType(type);
        }

        private Result DecodePayload(IMessage payload, ChannelOptions options)
        {
            var result = Result.Ok();
            foreach (var encoder in (Encoders as IEnumerable<MessageEncoder>).Reverse())
            {
                result = Result.Combine(result, encoder.Decode(payload, options));
            }

            return result;
        }

        /// <summary>Parse paginated response using specified parser function.</summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="funcParse">Function to parse HTTP response into a sequence of items.</param>
        /// <returns></returns>
        internal static PaginatedResult<T> Paginated<T>(AblyRequest request, AblyResponse response, Func<HistoryRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            PaginatedResult<T> res = new PaginatedResult<T>(response.Headers, GetLimit(request), executeDataQueryRequest);
            return res;
        }

        public PaginatedResult<T> ParsePaginatedResponse<T>(AblyRequest request, AblyResponse response, Func<HistoryRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            LogResponse(response);
            var result = Paginated(request, response, executeDataQueryRequest);
            var items = new List<T>();
            if (typeof(T) == typeof(Message))
            {
                var typedResult = result as PaginatedResult<Message>;
                typedResult.Items.AddRange(ParseMessagesResponse(response, request.ChannelOptions));
            }

            if (typeof(T) == typeof(Stats))
            {
                var typedResult = result as PaginatedResult<Stats>;
                typedResult?.Items.AddRange(ParseStatsResponse(response));
            }

            if (typeof(T) == typeof(PresenceMessage))
            {
                var typedResult = result as PaginatedResult<PresenceMessage>;
                typedResult.Items.AddRange(ParsePresenceMessages(response, request.ChannelOptions));
            }

            return result;
        }

        public T ParseResponse<T>(AblyRequest request, AblyResponse response) where T : class
        {
            LogResponse(response);

            var responseText = response.TextResponse;
#if MSGPACK
            if (_protocol == Protocol.MsgPack)
            {
                return (T)MsgPackHelper.Deserialise(response.Body, typeof(T));
            }
#endif
            return JsonHelper.Deserialize<T>(responseText);
        }

        private void LogResponse(AblyResponse response)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Protocol:" + _protocol);
                try
                {
                    var responseBody = response.TextResponse;
#if MSGPACK
                    if (_protocol == Protocol.MsgPack && response.Body != null)
                    {
                        responseBody = MsgPackHelper.DeserialiseMsgPackObject(response.Body).ToString();
                    }
#endif
                    Logger.Debug("Response: " + responseBody);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while logging response body.", ex);
                }
            }
        }

        private IEnumerable<Stats> ParseStatsResponse(AblyResponse response)
        {
            var body = response.TextResponse;
#if MSGPACK
            if (_protocol == Protocol.MsgPack)
            {
                return (List<Stats>)MsgPackHelper.Deserialise(response.Body, typeof(List<Stats>));
            }
#endif
            return JsonHelper.Deserialize<List<Stats>>(body);
        }

        private static int GetLimit(AblyRequest request)
        {
            if (request.QueryParameters.ContainsKey("limit"))
            {
                var limitQuery = request.QueryParameters["limit"];
                if (limitQuery.IsNotEmpty())
                {
                    return int.Parse(limitQuery);
                }
            }

            return Defaults.QueryLimit;
        }

        public ProtocolMessage ParseRealtimeData(RealtimeTransportData data)
        {
            ProtocolMessage protocolMessage;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (IsMsgPack() && Config.MsgPackEnabled)
            {
#if MSGPACK
                protocolMessage = (ProtocolMessage) MsgPackHelper.Deserialise(data.Data, typeof(ProtocolMessage));
#endif
            }
            else
            {
                protocolMessage = JsonHelper.Deserialize<ProtocolMessage>(data.Text);
            }

            if (protocolMessage != null)
            {
                foreach (var presenceMessage in protocolMessage.Presence)
                {
                    presenceMessage.Timestamp = protocolMessage.Timestamp;
                }

                foreach (var message in protocolMessage.Messages)
                {
                    message.Timestamp = protocolMessage.Timestamp;
                }
            }

            return protocolMessage;
        }

        public Result EncodeProtocolMessage(ProtocolMessage protocolMessage, ChannelOptions channelOptions)
        {
            var options = channelOptions ?? new ChannelOptions();
            var result = Result.Ok();
            foreach (var message in protocolMessage.Messages)
            {
                result = Result.Combine(result, EncodePayload(message, options));
            }

            foreach (var presence in protocolMessage.Presence)
            {
                result = Result.Combine(result, EncodePayload(presence, options));
            }

            return result;
        }

        public Result DecodeProtocolMessage(ProtocolMessage protocolMessage, ChannelOptions channelOptions)
        {
            var options = channelOptions ?? new ChannelOptions();

            return Result.Combine(
                DecodeMessages(protocolMessage, protocolMessage.Messages, options),
                DecodeMessages(protocolMessage, protocolMessage.Presence, options));
        }

        private Result DecodeMessages(ProtocolMessage protocolMessage, IEnumerable<IMessage> messages, ChannelOptions options)
        {
            var result = Result.Ok();
            var index = 0;
            foreach (var message in messages ?? Enumerable.Empty<IMessage>())
            {
                SetMessageIdConnectionIdAndTimestamp(protocolMessage, message, index);
                result = Result.Combine(result, DecodePayload(message, options));
                index++;
            }

            return result;
        }

        private static void SetMessageIdConnectionIdAndTimestamp(ProtocolMessage protocolMessage, IMessage message, int i)
        {
            if (message.Id.IsEmpty())
            {
                message.Id = $"{protocolMessage.Id}:{i}";
            }

            if (message.ConnectionId.IsEmpty())
            {
                message.ConnectionId = protocolMessage.ConnectionId;
            }

            if (message.Timestamp.HasValue == false)
            {
                message.Timestamp = protocolMessage.Timestamp;
            }
        }

        public RealtimeTransportData GetTransportData(ProtocolMessage protocolMessage)
        {
            RealtimeTransportData data;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (IsMsgPack() && Config.MsgPackEnabled)
            {
#if MSGPACK
                var bytes = MsgPackHelper.Serialise(protocolMessage);
                data = new RealtimeTransportData(bytes) {Original = protocolMessage};
#endif
            }
            else
            {
                var text = JsonHelper.Serialize(protocolMessage);
                data = new RealtimeTransportData(text) { Original = protocolMessage };
            }

            return data;
        }

        private bool IsMsgPack()
        {
            bool isMsgPack = false;
#if MSGPACK
            isMsgPack = _protocol == Protocol.MsgPack;
#endif
            return isMsgPack;
        }
    }
}
