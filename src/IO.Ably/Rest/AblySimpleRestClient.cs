using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IO.Ably.MessageEncoders;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    internal class AblySimpleRestClient : IAblyRest
    {
        public AblySimpleRestClient(ClientOptions options)
            : this(options, new AblyHttpClient(GetHost(options), options.Port, options.Tls, options.Environment))
        { }

        public AblySimpleRestClient(ClientOptions options, IAblyHttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
            _protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            _messageHandler = new MessageHandler(_protocol);
        }

        private ClientOptions _options;
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

        public Task<AblyResponse> ExecuteRequest(AblyRequest request)
        {
            if (!request.SkipAuthentication)
                throw new InvalidOperationException("AblySimpleRestClient does not support authenticated requests");

            _messageHandler.SetRequestBody(request);

            return _httpClient.Execute(request);
        }

        async public Task<T> ExecuteRequest<T>(AblyRequest request) where T : class
        {
            var response = await ExecuteRequest(request);
            return _messageHandler.ParseResponse<T>(request, response);
        }

        public async Task<DateTimeOffset> Time()
        {
            var request = CreateGetRequest("/time");
            request.SkipAuthentication = true;
            var response = await ExecuteRequest<List<long>>(request);

            return response.First().FromUnixTimeInMilliseconds();
        }

        private static string GetHost(ClientOptions options)
        {
            if (StringExtensions.IsNotEmpty(options.RestHost))
                return options.RestHost;

            return Config.DefaultHost;
        }
    }
}
