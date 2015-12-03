using System.Linq;
#if SILVERLIGHT
using SW = System.Windows.Browser;
using SCS = Ably.Utils;
#else
using SW = RestSharp.Extensions.MonoHttp;
using SCS = System.Collections.Specialized;
#endif

namespace Ably.Utils
{
    internal static class QueryStringUtility
    {
        public static SCS.NameValueCollection CreateQueryCollection()
        {
            return new SCS.NameValueCollection();
        }

        public static string ToQueryString(this SCS.NameValueCollection collection)
        {
            var array = (from key in collection.AllKeys
                         from value in collection.GetValues(key)
                         select string.Format("{0}={1}", SW.HttpUtility.UrlEncode(key), SW.HttpUtility.UrlEncode(value)))
                         .ToArray();
            return string.Join("&", array);
        }
    }
}
