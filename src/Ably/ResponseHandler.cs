using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

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
                //if (payload.IsBinaryMessage)
                //{
                //    yield return
                //        new Message()
                //        {
                //            Name = payload.name,
                //            //Data = payload.Data.FromBase64(),
                //            TimeStamp = GetTime(payload)
                //        };
                //}
                //else if (payload.IsEncrypted)
                //{
                //    if (options.Encrypted == false || options.CipherParams == null)
                //    {
                //        throw new AblyException("Cannot decrypt message because the current channel was created without encryption enabled. Payload: " + payload);
                //    }

                //    var cipher = GetCipher(options);
                //    var buffer = GetTypedBufferFromEncryptedMessage(payload, cipher);

                //    yield return new Message()
                //    {
                //        Name = payload.name,
                //        TimeStamp = GetTime(payload),
                //        Data = null
                //    };
                //}
                //else
                //{

                //    yield return new Message()
                //    {
                //        Name = payload.name,
                //        TimeStamp = GetTime(payload),
                //        //Data = payload.Data.IsJson() ? (object)JToken.Parse(payload.Data) : payload.Data
                //    };
                //}
            }

            return Enumerable.Empty<Message>();
        }

        private IChannelCipher GetCipher(ChannelOptions options)
        {
            return _cipher ?? (_cipher = Crypto.GetCipher(options));
        }

        private static DateTimeOffset GetTime(MessagePayload payload)
        {
            if (payload.timestamp.HasValue)
                return payload.timestamp.Value.FromUnixTime();
            return Config.Now();
        }
    }
}