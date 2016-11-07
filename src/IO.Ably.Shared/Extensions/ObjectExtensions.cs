using Newtonsoft.Json;

namespace IO.Ably
{
    internal static class ObjectExtensions
    {
        public static string ToJson(this object obj)
        {
            return JsonHelper.Serialize(obj);
        }
    }
}