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
        public RequestHandler()
        {
        }

        public byte[] GetRequestBody(AblyRequest request)
        {
            if (request.PostData is Message)
                return GetMessagesRequestBody(new[] { request.PostData as Message }, request.UseTextProtocol, request.Encrypted, request.CipherParams);
            if (request.PostData is IEnumerable<Message>)
                return GetMessagesRequestBody(request.PostData as IEnumerable<Message>, request.UseTextProtocol, request.Encrypted, request.CipherParams);
            return GetBytes(JsonConvert.SerializeObject(request.PostData));
        }

        private byte[] GetMessagesRequestBody(IEnumerable<Message> messages, bool useTextProtocol, bool encrytped, CipherParams @params)
        {

            if (useTextProtocol)
            {
                var payloads = new List<MessagePayload>();
                foreach (var message in messages)
                {
                    var payload = new MessagePayload()
                    {
                        Name = message.Name,
                        Timestamp = message.Timestamp
                    };

                    if (encrytped)
                    {
                        var cipher = Config.GetCipher(@params);
                        var data = Data.AsPlaintext(message.Data);
                        payload.Data = new CipherData(cipher.Encrypt(data.Buffer), data.Type);
                        payload.Encoding = "cipher+base64";
                    }
                    if (message.IsBinaryMessage)
                    {
                        payload.Data = Convert.ToBase64String(message.Value<byte[]>());
                        payload.Encoding = "base64";
                    }
                    else
                    {
                        payload.Data = message.Data;
                    }
                    payloads.Add(payload);
                }

                var text = payloads.Count == 1 ? JsonConvert.SerializeObject(payloads.First()) : JsonConvert.SerializeObject(payloads);
                return GetBytes(text);
            }
            throw new AblyException("Binary protocol is not supported yet.", 50000, HttpStatusCode.InternalServerError);
        }

        private byte[] GetBytes(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }
    }
}