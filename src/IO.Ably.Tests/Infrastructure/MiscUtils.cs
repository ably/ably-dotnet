using IO.Ably.Realtime;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    static class MiscUtils
    {
        public static string AddRandomSuffix(this string str)
        {
            if (str.IsEmpty()) return str;
            return str + "_" + Guid.NewGuid().ToString("D").Substring(0, 8);
        }
        public static Task<AblyResponse> ToAblyResponse(this string txt)
        {
            return Task.FromResult(new AblyResponse() { TextResponse = txt });
        }

        public static Task<AblyResponse> ToAblyJsonResponse(this string txt)
        {
            return Task.FromResult(new AblyResponse() { TextResponse = txt, Type = ResponseType.Json });
        }

        public static Task<AblyResponse> ToTask(this AblyResponse r)
        {
            return Task.FromResult(r);
        }

        public static void Subscribe(this IRealtimeChannel target, string eventName, Action<Message> act)
        {
            target.Subscribe(eventName, new MessageHandlerAction(act));
        }

        public static void Subscribe(this IRealtimeChannel target, Action<Message> act)
        {
            target.Subscribe(new MessageHandlerAction(act));
        }

        public static void Unsubscribe(this IRealtimeChannel target, string eventName, Action<Message[]> act)
        { }
    }
}