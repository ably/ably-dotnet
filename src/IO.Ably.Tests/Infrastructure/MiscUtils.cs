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

        static IMessageHandler Handler(Action<Message[]> act)
        {
            Action<Message> a2 = msg =>
            {
                Message[] arr = new Message[1] { msg };
                act(arr);
            };
            return new MessageHandlerAction(a2);
        }

        public static void Subscribe(this IRealtimeChannel target, string eventName, Action<Message[]> act)
        {
            target.Subscribe(eventName, Handler(act));
        }

        public static void Subscribe(this IRealtimeChannel target, Action<Message[]> act)
        {
            target.Subscribe(Handler(act));
        }

        public static void Unsubscribe(this IRealtimeChannel target, string eventName, Action<Message[]> act)
        { }
    }
}