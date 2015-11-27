namespace Ably.Utils
{
    internal class HttpUtility
    {
#if NET
        public static string UrlEncode(string str)
        {
            return System.Web.HttpUtility.UrlEncode(str);
        }
#elif SILVERLIGHT
        public static string UrlEncode(string str)
        {
            return System.Windows.Browser.HttpUtility.UrlEncode(str);
        }
#endif
    }
}
