using System;
using System.Collections.Generic;
using System.Net;
using Ably.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ably
{
    internal class ResponseHandler : IResponseHandler
    {
        private IChannelCipher _cipher;

        public T ParseMessagesResponse<T>(AblyResponse response) where T : class
        {
            if (response.Type == ResponseType.Json)
                return JsonConvert.DeserializeObject<T>(response.TextResponse);
            return default(T);
        }

        public IEnumerable<Message> ParseMessagesResponse(AblyResponse response, ChannelOptions options)
        {
            if (response.Type == ResponseType.Json)
            {
                var messages = JsonConvert.DeserializeObject<List<MessagePayload>>(response.TextResponse);

                return ProcessMessages(messages, options);
            }
            throw new AblyException("Only json messages are supported.", 50100, HttpStatusCode.NotImplemented);
        }

        private IEnumerable<Message> ProcessMessages(IEnumerable<MessagePayload> payloads, ChannelOptions options)
        {
            foreach (var payload in payloads)
            {
                if (payload.IsBinaryMessage)
                {
                    yield return
                        new Message()
                        {
                            Name = payload.Name,
                            Data = payload.Data.FromBase64(),
                            TimeStamp = GetTime(payload)
                        };
                }
                else if (payload.IsEncrypted)
                {
                    if (options.Encrypted == false || options.CipherParams == null)
                    {
                        throw new AblyException("Cannot decrypt message because the current channel was created without encryption enabled. Payload: " + payload);
                    }

                    var cipher = GetCipher(options);
                    var buffer = GetTypedBufferFromEncryptedMessage(payload, cipher);

                    yield return new Message()
                    {
                        Name = payload.Name,
                        TimeStamp = GetTime(payload),
                        Data = Data.FromPlaintext(buffer)
                    };
                }
                else
                {
                    
                    yield return new Message()
                    {
                        Name = payload.Name,
                        TimeStamp = GetTime(payload),
                        Data = payload.Data.IsJson() ? (object)JToken.Parse(payload.Data) : payload.Data
                    };
                }
            }
        }

        private IChannelCipher GetCipher(ChannelOptions options)
        {
            return _cipher ?? (_cipher = Crypto.GetCipher(options));
        }

        private static DateTime GetTime(MessagePayload payload)
        {
            if (payload.Timestamp.HasValue)
                return payload.Timestamp.Value.FromUnixTime();
            return Config.Now();
        }

        private static TypedBuffer GetTypedBufferFromEncryptedMessage(MessagePayload payload, IChannelCipher cipher)
        {
            TType type;
            if (Enum.TryParse(payload.Type, out type))
            {
                var result = new TypedBuffer() { Type = type };
                if (payload.Data.IsNotEmpty())
                {
                    try
                    {
                        result.Buffer = cipher.Decrypt(payload.Data.FromBase64());
                    }
                    catch (Exception ex)
                    {
                        throw new AblyException(string.Format("Cannot decrypt payload: {0}", payload));
                    }
                }
                return result;
            }
            else
            {
                throw new AblyException(string.Format("Data type was not supplied in the incoming message. Payload: {0}", payload), 50000, HttpStatusCode.BadRequest);
            }
        }
    }
}