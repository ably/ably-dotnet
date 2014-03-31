using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Ably
{
    internal class RequestHandler : IRequestHandler
    {
        public byte[] GetRequestBody(AblyRequest request)
        {
            if(request.UseTextProtocol == false)
                throw new AblyException("Binary protocol is not supported yet.", 50000, HttpStatusCode.InternalServerError);

            if (request.PostData is Message)
                return GetMessagesRequestBody(new[] {request.PostData as Message}, request.UseTextProtocol,
                    request.Encrypted, request.CipherParams);
            if (request.PostData is IEnumerable<Message>)
                return GetMessagesRequestBody(request.PostData as IEnumerable<Message>, request.UseTextProtocol,
                    request.Encrypted, request.CipherParams);
            
            return JsonConvert.SerializeObject(request.PostData).GetBytes();
        }

        byte[] GetMessagesRequestBody(IEnumerable<Message> messages, bool useTextProtocol, bool encrypted, CipherParams @params)
        {
                var payloads = messages.Select(message => CreateMessagePayload(message, encrypted, @params));

                var text = messages.Count() == 1 ? JsonConvert.SerializeObject(payloads.First()) : JsonConvert.SerializeObject(payloads);
                return text.GetBytes();
        }

        internal static MessagePayload CreateMessagePayload(Message message, bool encrypted, CipherParams @params)
        {
            var payload = new MessagePayload()
            {
                Name = message.Name,
                Timestamp = message.TimeStamp.DateTime.ToUnixTime()
            };

            if (encrypted)
            {
                var cipher = Config.GetCipher(@params);
                var data = Data.AsPlaintext(message.Data);
                payload.Data = new CipherData(cipher.Encrypt(data.Buffer), data.Type);
                payload.Encoding = "cipher+base64";
            }
            else if (message.IsBinaryMessage)
            {
                payload.Data = message.Value<byte[]>().ToBase64();
                payload.Encoding = "base64";
            }
            else
            {
                payload.Data = message.Data;
            }
            return payload;
        }
    }
}