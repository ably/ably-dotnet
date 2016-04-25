using Newtonsoft.Json;

namespace IO.Ably
{
    internal static class ObjectExtensions
    {
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Config.GetJsonSettings());
        }
    }
}