using IO.Ably;
using IO.Ably.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Ably.Tests
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

        public static void Subscribe( this Channel target, string eventName, Action<Message[]> act )
        {
            target.Subscribe( eventName, handler( act ) );
        }

        public static void sub( this Channel target, Action<Message[]> act )
        {
            target.Subscribe( handler( act ) );
        }

        public static void Unsubscribe( this Channel target, string eventName, Action<Message[]> act )
        { }
    }
}