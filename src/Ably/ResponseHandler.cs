using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Ably
{
    internal class ResponseHandler : IResponseHandler
    {
        static readonly IList<IObjectConverter> _converters = new List<IObjectConverter>();

        static ResponseHandler()
        {
            _converters.Add(new PartialMessageJsonConverter());
            _converters.Add(new MessageJsonConverter());
            _converters.Add(new MessageListJsonConverter());
        }

        private IObjectConverter GetConverter(object obj)
        {
            var type = obj.GetType();
            var converter = _converters.FirstOrDefault(x => x.CanHandleType(type));
            if(converter == null)
                throw new Exception("Cannot find converter for type " + type.FullName);

            return converter;
        }

        public T ParseResponse<T>(AblyResponse response) where T : class
        {
            if (response.Type == ResponseType.Json)
                return JsonConvert.DeserializeObject<T>(response.TextResponse);
            return default(T);
        }

        public T ParseResponse<T>(AblyResponse response, T obj) where T : class
        {
            if (response.Type == ResponseType.Json)
            {
                JsonConvert.PopulateObject(response.TextResponse, obj);
                return obj;
            }
            return obj;
        }
    }
}