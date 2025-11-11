using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.MessageEncoders
{
    internal class MessageHandler
    {
        private static readonly Base64Encoder Base64Encoder = new Base64Encoder();

        private static readonly Type[] UnsupportedTypes =
        {
            typeof(short), typeof(int), typeof(double), typeof(float), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(byte), typeof(bool),
            typeof(long), typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
        };

        private readonly Protocol _protocol;

        private static List<MessageEncoder> AllEncoders { get; } = new List<MessageEncoder>
        {
            new JsonEncoder(), new Utf8Encoder(), new CipherEncoder(), new VcDiffEncoder(), Base64Encoder,
        };

        private List<MessageEncoder> Encoders { get; }

        private ILogger Logger { get; }

        public MessageHandler(ILogger logger, Protocol protocol)
        {
            Logger = logger;
            _protocol = protocol;
            if (IsMsgPack())
            {
                Encoders = AllEncoders.Where(encoder => encoder != Base64Encoder).ToList(); // No need for Base64Encoder for MsgPack
            }
            else
            {
                Encoders = AllEncoders;
            }
        }

        private IEnumerable<PresenceMessage> ParsePresenceMessages(AblyResponse response, DecodingContext context)
        {
            if (response.Type == ResponseType.Json)
            {
                var messages = JsonHelper.Deserialize<List<PresenceMessage>>(response.TextResponse);
                ProcessMessages(messages, context);
                return messages;
            }

            var payloads = MsgPackHelper.Deserialise(response.Body, typeof(List<PresenceMessage>)) as List<PresenceMessage>;
            ProcessMessages(payloads, context);
            return payloads;
        }

        private IEnumerable<Message> ParseMessagesResponse(AblyResponse response, DecodingContext context)
        {
            if (response.Type == ResponseType.Json)
            {
                var messages = JsonHelper.Deserialize<List<Message>>(response.TextResponse);
                ProcessMessages(messages, context);
                return messages;
            }

            var payloads = MsgPackHelper.Deserialise(response.Body, typeof(List<Message>)) as List<Message>;
            ProcessMessages(payloads, context);
            return payloads;
        }

        private void ProcessMessages<T>(IEnumerable<T> payloads, DecodingContext context) where T : IMessage
        {
            // TODO: What happens with rest request where we can't decode messages
            _ = DecodePayloads(context, payloads as IEnumerable<IMessage>, Encoders);
        }

        public void SetRequestBody(AblyRequest request)
        {
            request.RequestBody = GetRequestBody(request);
            if (IsMsgPack() && Logger.IsDebug)
            {
                LogRequestBody(request.RequestBody);
            }
        }

        private void LogRequestBody(byte[] requestBody)
        {
            if (Logger.IsDebug && requestBody != null)
            {
                try
                {
                    var msgPackObject = MsgPackHelper.DecodeMsgPackObject(requestBody);
                    Logger.Debug("Request body (MsgPack): " + msgPackObject);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error logging request body", ex);
                }
            }
        }

        private byte[] GetRequestBody(AblyRequest request)
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
            if (IsMsgPack())
            {
                result = MsgPackHelper.Serialise(request.PostData);
            }
            else
            {
                result = JsonHelper.Serialize(request.PostData).GetBytes();
            }

            if (Logger.IsDebug)
            {
                Logger.Debug("Request body: " + result.GetText());
            }

            return result;
        }

        private byte[] GetMessagesRequestBody(IEnumerable<Message> payloads, ChannelOptions options)
        {
            EncodePayloads(new DecodingContext(options), payloads);
            byte[] result;
            if (IsMsgPack())
            {
                result = MsgPackHelper.Serialise(payloads);
            }
            else
            {
                result = JsonHelper.Serialize(payloads).GetBytes();
            }

            return result;
        }

        internal Result EncodePayloads(DecodingContext context, IEnumerable<IMessage> payloads)
        {
            var result = Result.Ok();
            foreach (var payload in payloads)
            {
                result = Result.Combine(result, EncodePayload(payload, context, Encoders));
            }

            return result;
        }

        internal Result EncodePayload(IMessage payload, DecodingContext context, IEnumerable<MessageEncoder> encoders)
        {
            ValidatePayloadDataType(payload);
            var result = Result.Ok();
            foreach (var encoder in encoders)
            {
                var encodeResult = encoder.Encode(payload, context);
                if (encodeResult.IsSuccess)
                {
                    payload.Data = encodeResult.Value.Data;
                    payload.Encoding = encodeResult.Value.Encoding;
                }

                result = Result.Combine(result, encodeResult);

                if (result.IsFailure)
                {
                    break;
                }
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
                throw new AblyException("Unsupported payload type. Only string, binary data (byte[]) and objects convertable to json are supported being directly sent. This ensures that libraries in different languages work correctly. To send the requested value please create a DTO and pass the DTO as payload. For example if you are sending an '10' then create a class with one property; assign the value to the property and send it.");
            }

            Type GetNullableType(Type type)
            {
                if (type.GetTypeInfo().IsValueType == false)
                {
                    return null; // ref-type
                }

                return Nullable.GetUnderlyingType(type);
            }
        }

        internal static Result DecodePayload(IMessage payload, DecodingContext context, IEnumerable<MessageEncoder> encoders = null, ILogger logger = null)
        {
            var actualEncoders = (encoders ?? AllEncoders).ToList();
            var pp = context.PreviousPayload; // We take a chance that this will not be modified but replaced

            var (processResult, decodedPayload) = Decode();

            // None of the encoders updated the PreviousPayload
            // then we need to set the default one
            Result overallResult = processResult;
            if (pp == context.PreviousPayload)
            {
                var originalPayloadResult = GetOriginalMessagePayload();
                _ = originalPayloadResult.IfSuccess(x =>
                {
                    context.PreviousPayload = x;
                });
                overallResult = Result.Combine(overallResult, originalPayloadResult);
            }

            payload.Data = decodedPayload.Data;
            payload.Encoding = decodedPayload.Encoding;

            return overallResult;

            Result<PayloadCache> GetOriginalMessagePayload()
            {
                if (payload.Data is byte[] data)
                {
                    return Result.Ok(new PayloadCache(data, payload.Encoding));
                }

                bool isFirstEncodingBase64 = MessageEncoder.CurrentEncodingIs(payload, Base64Encoder.EncodingNameStr);
                if (isFirstEncodingBase64)
                {
                    var result = Base64Encoder.Decode(payload, new DecodingContext());
                    return result.Map(x => new PayloadCache((byte[])x.Data, MessageEncoder.RemoveCurrentEncodingPart(payload)));
                }

                return Result.Ok(new PayloadCache((string)payload.Data, payload.Encoding));
            }

            // Local function that tidies the processing
            // the first part of the tuple will return the result. We don't have `true` or `false` because
            // we care about the error message that came from the encoder that failed.
            // The processed payload is returned separately so we can still update the message.
            (Result<Unit> processResult, IPayload processedPayload) Decode()
            {
                int processedEncodings = 0;
                var numberOfEncodings = payload.Encoding.IsNotEmpty() ? payload.Encoding.Count(x => x == '/') + 1 : 0;
                if (numberOfEncodings == 0 || payload.Data == null)
                {
                    return (Result.Ok(Unit.Default), payload);
                }

                IPayload currentPayload = payload;
                while (true)
                {
                    var currentEncoding = MessageEncoder.GetCurrentEncoding(currentPayload);
                    if (currentEncoding.IsEmpty())
                    {
                        return (Result.Ok(Unit.Default), currentPayload);
                    }

                    var decoder = actualEncoders.FirstOrDefault(x => x.CanProcess(currentEncoding));
                    if (decoder == null)
                    {
                        logger?.Warning($"Missing decoder for '{currentEncoding}'. Leaving as it is");
                        return (Result.Ok(Unit.Default), currentPayload);
                    }

                    var result = decoder.Decode(currentPayload, context);

                    if (result.IsSuccess)
                    {
                        currentPayload = result.Value;
                    }
                    else
                    {
                        // If an encoder fails we want to return the result up to this encoder
                        return (Result.Fail<Unit>(result), currentPayload);
                    }

                    // just to be safe
                    if (processedEncodings > numberOfEncodings)
                    {
                        // TODO: Send to Sentry
                        return (Result.Fail<Unit>(new ErrorInfo("Failed to decode message encoding")), currentPayload);
                    }

                    processedEncodings++;
                }
            }
        }

        public PaginatedResult<T> ParsePaginatedResponse<T>(AblyRequest request, AblyResponse response, Func<PaginatedRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            LogResponse(response);
            var result = Paginated(request, response, executeDataQueryRequest);

            if (typeof(T) == typeof(Message))
            {
                var typedResult = result as PaginatedResult<Message>;
                var context = request.ChannelOptions.ToDecodingContext();
                typedResult?.Items.AddRange(ParseMessagesResponse(response, context));
            }
            else if (typeof(T) == typeof(PresenceMessage))
            {
                var typedResult = result as PaginatedResult<PresenceMessage>;
                var context = request.ChannelOptions.ToDecodingContext();
                typedResult?.Items.AddRange(ParsePresenceMessages(response, context));
            }
            else
            {
                result?.Items.AddRange(ParseOther<T>(response, _protocol));
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

            if (IsMsgPack())
            {
                return (T)MsgPackHelper.Deserialise(response.Body, typeof(T));
            }

            return JsonHelper.Deserialize<T>(response.TextResponse);
        }

        private Result DecodePayloads(DecodingContext context, IEnumerable<IMessage> payloads, IEnumerable<MessageEncoder> encoders)
        {
            var result = Result.Ok();
            foreach (var payload in payloads)
            {
                result = Result.Combine(result, DecodePayload(payload, context, encoders));
            }

            return result;
        }

        private static PaginatedResult<T> Paginated<T>(AblyRequest request, AblyResponse response, Func<PaginatedRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            PaginatedResult<T> res = new PaginatedResult<T>(response, GetLimit(request), executeDataQueryRequest);
            return res;
        }

        private void LogResponse(AblyResponse response)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Protocol: {_protocol}");
                try
                {
                    var responseBody = response.TextResponse;
                    if (IsMsgPack() && response.Body != null)
                    {
                        responseBody = MsgPackHelper.DecodeMsgPackObject(response.Body);
                    }

                    Logger.Debug($"Response: {responseBody}");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while logging response body.", ex);
                }
            }
        }

        private static IEnumerable<T> ParseOther<T>(AblyResponse response, Protocol protocol)
        {
            var body = response.TextResponse;
            if (protocol == Protocol.MsgPack)
            {
                return (List<T>)MsgPackHelper.Deserialise(response.Body, typeof(List<T>));
            }

            return JsonHelper.Deserialize<List<T>>(body) ?? new List<T>();
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

            if (IsMsgPack())
            {
                protocolMessage = (ProtocolMessage)MsgPackHelper.Deserialise(data.Data, typeof(ProtocolMessage));
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

        public Result EncodeProtocolMessage(ProtocolMessage protocolMessage, DecodingContext context)
        {
            var result = Result.Ok();
            if (protocolMessage.Messages != null)
            {
                result = Result.Combine(EncodePayloads(context, protocolMessage.Messages));
            }

            if (protocolMessage.Presence != null)
            {
                result = Result.Combine(EncodePayloads(context, protocolMessage.Presence));
            }

            return result;
        }

        public Result DecodeMessages(
            ProtocolMessage protocolMessage,
            IEnumerable<IMessage> messages,
            ChannelOptions options)
        {
            var context = options.ToDecodingContext();
            return DecodeMessages(protocolMessage, messages, context);
        }

        public Result DecodeMessages(
            ProtocolMessage protocolMessage,
            IEnumerable<IMessage> messages,
            DecodingContext context)
        {
            var result = Result.Ok();
            var index = 0;
            foreach (var message in messages ?? Enumerable.Empty<IMessage>())
            {
                SetMessageIdConnectionIdAndTimestamp(message, index);
                var decodeResult = DecodePayload(message, context, Encoders)
                    .IfFailure(error => Logger.Warning($"Error decoding message with id: {message.Id}. Error: {error.Message}. Exception: {error.InnerException?.Message}"));

                result = Result.Combine(result, decodeResult);

                if (result.IsFailure)
                {
                    break;
                }

                index++;
            }

            return result;

            void SetMessageIdConnectionIdAndTimestamp(IMessage message, int i)
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
        }

        public RealtimeTransportData GetTransportData(ProtocolMessage protocolMessage)
        {
            RealtimeTransportData data;

            if (IsMsgPack())
            {
                var bytes = MsgPackHelper.Serialise(protocolMessage);
                data = new RealtimeTransportData(bytes) { Original = protocolMessage };
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
            return _protocol == Protocol.MsgPack;
        }

        internal static T FromEncoded<T>(T encoded, ChannelOptions options = null)
            where T : IMessage
        {
            var context = options.ToDecodingContext();
            var result = DecodePayload(encoded, context, AllEncoders, DefaultLogger.LoggerInstance);
            if (result.IsFailure)
            {
                throw new AblyException(result.Error);
            }

            return encoded;
        }

        internal static T[] FromEncodedArray<T>(T[] encodedArray, ChannelOptions options = null)
            where T : IMessage
        {
            var context = options.ToDecodingContext();
            foreach (var encoded in encodedArray)
            {
                DecodePayload(encoded, context, AllEncoders, DefaultLogger.LoggerInstance);
            }

            return encodedArray;
        }
    }
}
