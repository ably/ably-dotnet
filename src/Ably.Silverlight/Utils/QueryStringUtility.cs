using System.Linq;
using System.Windows.Browser;

namespace Ably.Utils
{
    internal static class QueryStringUtility
    {
        public static NameValueCollection CreateQueryCollection()
        {
            return new NameValueCollection();
        }

        public static string ToQueryString(this NameValueCollection collection)
        {
            var array = (from key in collection.AllKeys
                         from value in collection.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                         .ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
