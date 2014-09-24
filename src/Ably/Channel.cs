using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Ably
{
    internal interface IResponseHandler
    {
        T ParseMessagesResponse<T>(AblyResponse response) where T : class;
        IEnumerable<Message> ParseMessagesResponse(AblyResponse response, ChannelOptions options);
    }

    internal interface IRequestHandler
    {
        byte[] GetRequestBody(AblyRequest request);
    }

    public class Channel : IChannel
    {
        public string Name { get; private set; }
        private readonly Rest _restClient;
        private readonly IResponseHandler _handler;
        private readonly ChannelOptions _options;
        private readonly string basePath;

        internal Channel(Rest restClient, string name, IResponseHandler handler,  ChannelOptions options)
        {
            Name = name;
            _handler = handler;
            _restClient = restClient;
            _options = GetOptions(options);
            basePath = string.Format("/channels/{0}", name.EncodeUriPart());
        }

        private ChannelOptions GetOptions(ChannelOptions options)
        {
            if(options == null)
                return new ChannelOptions();

            if (options.Encrypted && options.CipherParams == null)
            {
                return new ChannelOptions() {Encrypted = true, CipherParams = Crypto.GetDefaultParams()};
            }
            return new ChannelOptions() {Encrypted = options.Encrypted, CipherParams = options.CipherParams};
        }

        public void Publish(string name, object data)
        {
            var request = _restClient.CreatePostRequest(basePath + "/messages", _options.Encrypted, _options.CipherParams);

            request.PostData = new Message() { Name = name, Data = data };
            _restClient.ExecuteRequest(request);
        }

        public void Publish(IEnumerable<Message> messages)
        {
            var request = _restClient.CreatePostRequest(basePath + "/messages", _options.Encrypted, _options.CipherParams);
            request.PostData = messages;
            _restClient.ExecuteRequest(request);
        }

        public IList<PresenceMessage> Presence()
        {
            var request = _restClient.CreateGetRequest(basePath + "/presence");
            var response = _restClient.ExecuteRequest(request);

            return _handler.ParseMessagesResponse<List<PresenceMessage>>(response);
        }

        public IPartialResult<Message> History()
        {
            return History(new HistoryDataRequestQuery());
        }

        public IPartialResult<Message> History(DataRequestQuery query)
        {
            query.Validate();

            var request = _restClient.CreateGetRequest(basePath + "/messages");

            request.AddQueryParameters(query.GetParameters());

            var response = _restClient.ExecuteRequest(request);

            var result = new PartialResult<Message>(limit: query.Limit ?? 100);
            result.CurrentResultQuery = DataRequestQuery.Parse(response.Headers["current"]);
            result.NextQuery = DataRequestQuery.Parse(response.Headers["next"]);
            result.InitialResultQuery = DataRequestQuery.Parse(response.Headers["initial"]);

            result.AddRange(_handler.ParseMessagesResponse(response, _options));
            return result;
        }

        public IPartialResult<Message> History(HistoryDataRequestQuery query)
        {
            return History(query as DataRequestQuery);
        }
    }
}
