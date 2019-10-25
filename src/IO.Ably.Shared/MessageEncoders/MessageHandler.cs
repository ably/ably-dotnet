using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Newtonsoft.Json.Linq;

namespace IO.Ably.MessageEncoders
{
    internal class MessageHandler
    {
        internal ILogger Logger { get; private set; }

        public static List<MessageEncoder> Encoders
        {
            get
            {
                if (_encoders == null)
                {
                    InitializeMessageEncoders();
                }

                return _encoders;
            }
        }

        private static readonly Type[] UnsupportedTypes = new[]
            {
                typeof(short), typeof(int), typeof(double), typeof(float), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(byte), typeof(bool),
                typeof(long), typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte)
            };

        private static object _syncLock = new object();
        private readonly Protocol _protocol;
        public static List<MessageEncoder> _encoders = null;

        public MessageHandler()
            : this(DefaultLogger.LoggerInstance, Defaults.Protocol) { }

        public MessageHandler(Protocol protocol)
            : this(DefaultLogger.LoggerInstance, protocol) { }

        public MessageHandler(ILogger logger, Protocol protocol)
        {
            Logger = logger;
            _protocol = protocol;

            InitializeMessageEncoders();
        }

        private static void InitializeMessageEncoders()
        {
            if (_encoders != null)
            {
                return;
            }

            lock (_syncLock)
            {
                _encoders = new List<MessageEncoder>
                {
                    new JsonEncoder(), new Utf8Encoder(), new CipherEncoder(), new Base64Encoder()
                };
            }
        }

        public IEnumerable<PresenceMessage> ParsePresenceMessages(AblyResponse response, ChannelOptions options)
        {
            if (response.Type != ResponseType.Json)
            {
                throw new AblyException(
                    $"Response of type '{response.Type}' is invalid because MsgPack support was not enabled for this build.");
            }

            var messages = JsonHelper.Deserialize<List<PresenceMessage>>(response.TextResponse);
            ProcessMessages(messages, options);
            return messages;

#if MSGPACK
            var payloads = MsgPackHelper.Deserialise(response.Body, typeof(List<PresenceMessage>)) as List<PresenceMessage>;
            ProcessMessages(payloads, options);
            return payloads;
#else

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
            if (_protocol == Protocol.Json || !Defaults.MsgPackEnabled)
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

        internal static Result DecodePayloads(ChannelOptions options, IEnumerable<IMessage> payloads)
        {
            var result = Result.Ok();
            foreach (var payload in payloads)
            {
                result = Result.Combine(result, DecodePayload(payload, options));
            }

            return result;
        }

        private static Result EncodePayload(IMessage payload, ChannelOptions options)
        {
            ValidatePayloadDataType(payload);
            var result = Result.Ok();
            foreach (var encoder in Encoders)
            {
                result = Result.Combine(result, encoder.Encode(payload, options));
            }

            return result;
        }

        private static void ValidatePayloadDataType(IMessage payload)
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

        private static Result DecodePayload(IMessage payload, ChannelOptions options)
        {
            var result = Result.Ok();
            foreach (var encoder in (Encoders as IEnumerable<MessageEncoder>).Reverse())
            {
                result = Result.Combine(result, encoder.Decode(payload, options));
            }

            return result;
        }

        internal static PaginatedResult<T> Paginated<T>(AblyRequest request, AblyResponse response, Func<PaginatedRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            PaginatedResult<T> res = new PaginatedResult<T>(response, GetLimit(request), executeDataQueryRequest);
            return res;
        }

        public PaginatedResult<T> ParsePaginatedResponse<T>(AblyRequest request, AblyResponse response, Func<PaginatedRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            LogResponse(response);
            var result = Paginated(request, response, executeDataQueryRequest);
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

        public HttpPaginatedResponse ParseHttpPaginatedResponse(AblyRequest request, AblyResponse response, PaginatedRequestParams requestParams, Func<PaginatedRequestParams, Task<HttpPaginatedResponse>> executeDataQueryRequest)
        {
            LogResponse(response);
            return new HttpPaginatedResponse(response, GetLimit(request), requestParams, executeDataQueryRequest);
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
            if (IsMsgPack() && Defaults.MsgPackEnabled)
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
            if (protocolMessage.Messages != null)
            {
                foreach (var message in protocolMessage.Messages)
                {
                    result = Result.Combine(result, EncodePayload(message, options));
                }
            }

            if (protocolMessage.Presence != null)
            {
                foreach (var presence in protocolMessage.Presence)
                {
                    result = Result.Combine(result, EncodePayload(presence, options));
                }
            }

            return result;
        }

        public static Result DecodeProtocolMessage(ProtocolMessage protocolMessage, ChannelOptions channelOptions)
        {
            var options = channelOptions ?? new ChannelOptions();

            return Result.Combine(
                DecodeMessages(protocolMessage, protocolMessage.Messages, options),
                DecodeMessages(protocolMessage, protocolMessage.Presence, options));
        }

        private static Result DecodeMessages(ProtocolMessage protocolMessage, IEnumerable<IMessage> messages, ChannelOptions options)
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
            if (IsMsgPack() && Defaults.MsgPackEnabled)
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

        internal static T FromEncoded<T>(T encoded, ChannelOptions options = null) where T : IMessage
        {
            var result = DecodePayload(encoded, options);
            if (result.IsFailure)
            {
                throw new AblyException(result.Error);
            }

            return encoded;
        }

        internal static T[] FromEncodedArray<T>(T[] encodedArray, ChannelOptions options = null) where T : IMessage
        {
            foreach (var encoded in encodedArray)
            {
                DecodePayload(encoded, options);
            }

            return encodedArray;
        }
    }
}
