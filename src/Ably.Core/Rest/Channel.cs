using System.Collections.Generic;
using IO.Ably.Encryption;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    /// <summary>
    /// The Ably Realtime service organises the traffic within any application into named channels.
    /// Channels are the "unit" of message distribution; clients attach to channels to subscribe to messages,
    /// and every message broadcast by the service is associated with a unique channel.
    /// A channel cannot be instantiated but needs to be created using the AblyRest.Channels.Get("channelname")
    /// </summary>
    public class Channel : IChannel
    {
        public string Name { get; private set; }
        private readonly AblyRest _ablyRest;
        private readonly ChannelOptions _options;
        private readonly string basePath;

        internal Channel(AblyRest ablyRest, string name,  ChannelOptions options)
        {
            Name = name;
            _ablyRest = ablyRest;
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

        /// <summary>
        /// Publish a message to the channel
        /// </summary>
        /// <param name="name">The event name of the message to publish</param>
        /// <param name="data">The message payload. Allowed payloads are string, objects and byte[]</param>
        public void Publish(string name, object data)
        {
            var request = _ablyRest.RestMethods.CreatePostRequest(basePath + "/messages", _options);

            request.PostData = new List<Message> { new Message(name, data)};
            _ablyRest.RestMethods.ExecuteRequest(request);
        }

        /// <summary>
        /// Publish a list of messages to the channel
        /// </summary>
        /// <param name="messages">a list of messages</param>
        public void Publish(IEnumerable<Message> messages)
        {
            var request = _ablyRest.RestMethods.CreatePostRequest(basePath + "/messages", _options);
            request.PostData = messages;
            _ablyRest.RestMethods.ExecuteRequest(request);
        }

        /// <summary>
        /// Obtain the set of members currently present for a channel
        /// </summary>
        /// <returns><see cref="PaginatedResource{T}"/> of the PresenseMessages</returns>
        public Task<PaginatedResource<PresenceMessage>> Presence()
        {
            var request = _ablyRest.RestMethods.CreateGetRequest(basePath + "/presence", _options);
            return _ablyRest.RestMethods.ExecuteRequest<PaginatedResource<PresenceMessage>>(request);
        }

        /// <summary>
        /// Get the presence messages history for the channel
        /// </summary>
        /// <returns><see cref="PaginatedResource{PresenceMessage}"/></returns>
        public Task<PaginatedResource<PresenceMessage>> PresenceHistory()
        {
            var request = _ablyRest.RestMethods.CreateGetRequest(basePath + "/presence", _options);
            return _ablyRest.RestMethods.ExecuteRequest<PaginatedResource<PresenceMessage>>(request);
        }

        /// <summary>
        /// Get the presence messages history for the channel by specifying a query
        /// </summary>
        /// <returns><see cref="PaginatedResource{PresenceMessage}"/></returns>
        public Task<PaginatedResource<PresenceMessage>> PresenceHistory(DataRequestQuery query)
        {
            var request = _ablyRest.RestMethods.CreateGetRequest(basePath + "/presence", _options);
            request.AddQueryParameters(query.GetParameters());
            return _ablyRest.RestMethods.ExecuteRequest<PaginatedResource<PresenceMessage>>(request);
        }

        /// <summary>
        /// Return the message history of the channel
        /// </summary>
        /// <returns><see cref="PaginatedResource{T}"/> of Messages</returns>
        public Task<PaginatedResource<Message>> History()
        {
            return History(new DataRequestQuery());
        }

        /// <summary>
        /// Return the message history of the channel
        /// </summary>
        /// <param name="query"><see cref="DataRequestQuery"/></param>
        /// <returns><see cref="PaginatedResource{T}"/> of Messages</returns>
        public Task<PaginatedResource<Message>> History(DataRequestQuery query)
        {
            query.Validate();

            var request = _ablyRest.RestMethods.CreateGetRequest(basePath + "/messages", _options);

            request.AddQueryParameters(query.GetParameters());

            return _ablyRest.RestMethods.ExecuteRequest<PaginatedResource<Message>>(request);
        }
    }
}
