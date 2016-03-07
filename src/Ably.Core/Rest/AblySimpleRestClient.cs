using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IO.Ably.MessageEncoders;

namespace IO.Ably.Rest
{
    internal class AblySimpleRestClient : IAblyRest
    {
        public AblySimpleRestClient(AblyOptions options)
            : this(options, new AblyHttpClient(GetHost(options), options.Port, options.Tls, options.Environment))
        { }

        public AblySimpleRestClient(AblyOptions options, IAblyHttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
            _protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            _messageHandler = new MessageHandler(_protocol);
        }

        private AblyOptions _options;
        private Protocol _protocol;
        private MessageHandler _messageHandler;
        private IAblyHttpClient _httpClient;

        public AblyRequest CreateGetRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Get, _protocol) { ChannelOptions = options };
        }

        public AblyRequest CreatePostRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Post, _protocol) { ChannelOptions = options };
        }

        public AblyResponse ExecuteRequest(AblyRequest request)
        {
            if (!request.SkipAuthentication)
                throw new InvalidOperationException("AblySimpleRestClient does not support authenticated requests");

            _messageHandler.SetRequestBody(request);

            return _httpClient.Execute(request);
        }

        public T ExecuteRequest<T>(AblyRequest request) where T : class
        {
            var response = ExecuteRequest(request);
            return _messageHandler.ParseResponse<T>(request, response);
        }

        public DateTime Time()
        {
            var request = CreateGetRequest("/time");
            request.SkipAuthentication = true;
            var response = ExecuteRequest<List<long>>(request);

            return response.First().FromUnixTimeInMilliseconds();
        }

        private static string GetHost(AblyOptions options)
        {
            if (options.Host.IsNotEmpty())
                return options.Host;

            return Config.DefaultHost;
        }
    }
}
