using Newtonsoft.Json;

namespace Ably
{
    internal class ResponseHandler : IResponseHandler
    {
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