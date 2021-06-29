using System.Reflection;
using IO.Ably;
using IO.Ably.Push;

namespace DotnetPush
{
    public static class AblyExtensions
    {
        public static PushRealtime GetPushRealtime(this AblyRealtime realtime)
        {
            var pushProperty = realtime.GetType().GetProperty("Push", BindingFlags.Instance | BindingFlags.NonPublic);
            return (PushRealtime)pushProperty.GetValue(realtime);
        }
    }
}