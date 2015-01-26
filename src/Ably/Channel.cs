using System.Collections.Generic;

namespace Ably
{
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
            var request = _restClient.CreatePostRequest(basePath + "/messages", _options);

            request.PostData = new List<Message> { new Message(name, data)};
            _restClient.ExecuteRequest(request);
        }

        public void Publish(IEnumerable<Message> messages)
        {
            var request = _restClient.CreatePostRequest(basePath + "/messages", _options);
            request.PostData = messages;
            _restClient.ExecuteRequest(request);
        }

        public IPaginatedResource<PresenceMessage> Presence()
        {
            var request = _restClient.CreateGetRequest(basePath + "/presence", _options);
            return _restClient.ExecuteRequest<PaginatedResource<PresenceMessage>>(request);
        }

        public IPaginatedResource<PresenceMessage> PresenceHistory()
        {
            var request = _restClient.CreateGetRequest(basePath + "/presence", _options);
            return _restClient.ExecuteRequest<PaginatedResource<PresenceMessage>>(request);
        }

        public IPaginatedResource<PresenceMessage> PresenceHistory(DataRequestQuery query)
        {
            var request = _restClient.CreateGetRequest(basePath + "/presence", _options);
            request.AddQueryParameters(query.GetParameters());
            return _restClient.ExecuteRequest<PaginatedResource<PresenceMessage>>(request);
        }

        public IPaginatedResource<Message> History()
        {
            return History(new DataRequestQuery());
        }

        public IPaginatedResource<Message> History(DataRequestQuery query)
        {
            query.Validate();

            var request = _restClient.CreateGetRequest(basePath + "/messages", _options);

            request.AddQueryParameters(query.GetParameters());

            return _restClient.ExecuteRequest<PaginatedResource<Message>>(request);
        }
    }
}
