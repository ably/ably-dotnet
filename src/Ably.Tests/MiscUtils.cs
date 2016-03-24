using IO.Ably.Realtime;
using System;
using System.Threading.Tasks;

namespace IO.Ably
{
    static class MiscUtils
    {
        public static Task<AblyResponse> response( this string txt )
        {
            return Task<AblyResponse>.FromResult( new AblyResponse() { TextResponse = txt } );
        }

        public static Task<AblyResponse> jsonResponse( this string txt )
        {
            return Task<AblyResponse>.FromResult( new AblyResponse() { TextResponse = txt, Type = ResponseType.Json } );
        }

        public static Task<AblyResponse> task( this AblyResponse r )
        {
            return Task<AblyResponse>.FromResult( r );
        }

        static IMessageHandler handler( Action<Message[]> act )
        {
            Action<Message> a2 = msg =>
            {
                Message[] arr = new Message[1] { msg };
                act( arr );
            };
            return new MessageHandlerAction( a2 );
        }

        public static void Subscribe( this IRealtimeChannel target, string eventName, Action<Message[]> act )
        {
            target.Subscribe( eventName, handler( act ) );
        }

        public static void sub( this IRealtimeChannel target, Action<Message[]> act )
        {
            target.Subscribe( handler( act ) );
        }

        public static void Unsubscribe( this IRealtimeChannel target, string eventName, Action<Message[]> act )
        { }
    }
}