using System;
using System.Collections;
using System.Collections.Generic;
using Ably.Realtime;
using Thrift.Transport;

namespace Ably
{
    public static class Defaults
    {
        public const int Limit = 100;
        public static int ProtocolVersion     = 1;
	    public static string[] FallbackHosts = {"A.ably-realtime.com", "B.ably-realtime.com", "C.ably-realtime.com", "D.ably-realtime.com", "E.ably-realtime.com"};

        public static string[] GetFallbackHosts(AblyOptions options)
        {
            return options.Host.IsEmpty() ? FallbackHosts : new string[]{};
        }
	    public static string Host             = "rest.ably.io";
	    public const int Port                = 80;
	    public const int TlsPort             = 443;
	    public const int ConnectTimeout      = 15000;
	    public const int DisconnectTimeout   = 10000;
	    public const int SuspendedTimeout    = 60000;
	    public const int CometRecvTimeout    = 90000;
	    public const int CometSendTimeout    = 10000;
	    public static string[] Transports     = {"web_socket", "comet"};
        

        public static String GetHost(AblyOptions options)
        {
            return options.Host.IsNotEmpty() ? options.Host : Host;
        }
        
        public static int GetPort(AblyOptions options)
        {
            return options.Tls
                ? ((options.TlsPort.HasValue) ? options.TlsPort.Value : TlsPort)
                : ((options.Port.HasValue) ? options.Port.Value : Port);
        }

    }

    public interface IPartialResult<out T> : IEnumerable<T>
    {
        bool HasNext { get; }
        DataRequestQuery NextQuery { get; }
        DataRequestQuery InitialResultQuery { get; }
        DataRequestQuery CurrentResultQuery { get; }
    }

    public class PartialResult<T> : List<T>, IPartialResult<T>
    {
        private readonly int _limit;

        public PartialResult(int limit = Defaults.Limit)
        {
            _limit = limit;
        }

        public bool HasNext { get { return Count > _limit; } }
        public DataRequestQuery NextQuery { get; set; }
        public DataRequestQuery InitialResultQuery { get; set; }
        public DataRequestQuery CurrentResultQuery { get; set; }
    }
}