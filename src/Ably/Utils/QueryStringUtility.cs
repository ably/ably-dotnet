using RestSharp.Extensions.MonoHttp;
using System.Collections.Specialized;

namespace Ably.Utils
{
    internal static class QueryStringUtility
    {
        public static NameValueCollection CreateQueryCollection()
        {
            return HttpUtility.ParseQueryString("");
        }

        public static string ToQueryString(NameValueCollection collection)
        {
            return collection.ToString();
        }
    }
}
