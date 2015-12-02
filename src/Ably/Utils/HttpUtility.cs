namespace Ably.Utils
{
    internal class HttpUtility
    {
#if SILVERLIGHT
        public static string UrlEncode(string str)
        {
            return System.Windows.Browser.HttpUtility.UrlEncode(str);
        }
#else
        public static string UrlEncode(string str)
        {
            return System.Web.HttpUtility.UrlEncode(str);
        }
#endif
    }
}
