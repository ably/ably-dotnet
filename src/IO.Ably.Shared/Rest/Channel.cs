using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace IO.Ably.Rest
{
    /// <summary>
    /// The Ably Realtime service organises the traffic within any application into named channels.
    /// Channels are the "unit" of message distribution; clients attach to channels to subscribe to messages,
    /// and every message broadcast by the service is associated with a unique channel.
    /// A channel cannot be instantiated but needs to be created using the AblyRest.Channels.Get("channelname")
    /// </summary>
    public class RestChannel : IRestChannel, IPresence
    {
        private const int IdempotentGeneratedIdLength = 9;

        public string Name { get; private set; }

        private readonly AblyRest _ablyRest;

        public ChannelOptions Options
        {
            get => _options;
            set => _options = value ?? new ChannelOptions();
        }

        private readonly string _basePath;
        private ChannelOptions _options;

        internal RestChannel(AblyRest ablyRest, string name, ChannelOptions options)
        {
            Name = name;
            _ablyRest = ablyRest;
            _options = options;
            _basePath = $"/channels/{name.EncodeUriPart()}";
        }

        /// <summary>
        /// Publish a message to the channel
        /// </summary>
        /// <param name="name">The event name of the message to publish</param>
        /// <param name="data">The message payload. Allowed payloads are string, objects and byte[]</param>
        /// <param name="clientId">Explicit message clientId</param>
        public Task PublishAsync(string name, object data, string clientId = null)
        {
            var request = _ablyRest.CreatePostRequest(_basePath + "/messages", Options);

            request.PostData = new List<Message> { new Message(name, data, clientId) };
            return _ablyRest.ExecuteRequest(request);
        }

        public Task PublishAsync(Message message)
        {
            return PublishAsync(new[] { message });
        }

        /// <summary>
        /// Publish a list of messages to the channel
        /// </summary>
        /// <param name="messages">a list of messages</param>
        public Task PublishAsync(IEnumerable<Message> messages)
        {
            var result = _ablyRest.AblyAuth.ValidateClientIds(messages);
            if (result.IsFailure)
            {
                throw new AblyException(result.Error);
            }

            var request = _ablyRest.CreatePostRequest(_basePath + "/messages", Options);

            // if idempotentRestPublishing is enabled
            if (_ablyRest.Options.IdempotentRestPublishing)
            {
                // and all Messages have an empty id attribute
                if (messages.All(m => m.Id == null))
                {
                    // generate a base id string by base64-encoding a sequence of at least 9 bytes
                    var b = new byte[IdempotentGeneratedIdLength];
                    new Random().NextBytes(b);
                    var baseId = Convert.ToBase64String(b);
                    int serial = 0;
                    foreach (var message in messages)
                    {
                        // each message gets a unique id of the form <base id>:<serial>
                        message.Id = $"{baseId}:{serial}";
                        serial++;
                    }
                }
            }

            request.PostData = messages;
            return _ablyRest.ExecuteRequest(request);
        }

        public IPresence Presence => this;

        /// <summary>
        /// Obtain the set of members currently present for a channel
        /// </summary>
        /// <returns><see cref="PaginatedResult{T}"/> of the PresenseMessages</returns>
        Task<PaginatedResult<PresenceMessage>> IPresence.GetAsync(int? limit, string clientId, string connectionId)
        {
            if (limit.HasValue && (limit < 0 || limit > 1000))
            {
                throw new ArgumentException("Limit must be between 0 and 1000", nameof(limit));
            }

            var presenceLimit = limit ?? Defaults.QueryLimit;

            var request = _ablyRest.CreateGetRequest(_basePath + "/presence", Options);

            request.QueryParameters.Add("limit", presenceLimit.ToString());
            if (clientId.IsNotEmpty())
            {
                request.QueryParameters.Add("clientId", clientId);
            }

            if (connectionId.IsNotEmpty())
            {
                request.QueryParameters.Add("connectionId", connectionId);
            }

            return _ablyRest.ExecutePaginatedRequest(request, Presence.GetAsync);
        }

        Task<PaginatedResult<PresenceMessage>> IPresence.GetAsync(PaginatedRequestParams query)
        {
            if (query == null)
            {
                // Fall back on the default implementation
                return Presence.GetAsync();
            }

            query.Validate();

            var request = _ablyRest.CreateGetRequest(_basePath + "/presence", Options);
            request.AddQueryParameters(query.GetParameters());
            return _ablyRest.ExecutePaginatedRequest(request, Presence.GetAsync);
        }

        /// <summary>
            /// Get the presence messages history for the channel
            /// </summary>
            /// <returns><see cref="PaginatedResult{T}"/></returns>
        Task<PaginatedResult<PresenceMessage>> IPresence.HistoryAsync()
        {
            return Presence.HistoryAsync(new PaginatedRequestParams());
        }

        /// <summary>
        /// Get the presence messages history for the channel by specifying a query
        /// </summary>
        /// <returns><see cref="PaginatedResult{T}"/></returns>
        Task<PaginatedResult<PresenceMessage>> IPresence.HistoryAsync(PaginatedRequestParams query)
        {
            query = query ?? new PaginatedRequestParams();

            query.Validate();

            var request = _ablyRest.CreateGetRequest(_basePath + "/presence/history", Options);
            request.AddQueryParameters(query.GetParameters());
            return _ablyRest.ExecutePaginatedRequest(request, Presence.HistoryAsync);
        }

        /// <summary>
        /// Return the message history of the channel
        /// </summary>
        /// <returns><see cref="PaginatedResult{T}"/> of Messages</returns>
        public Task<PaginatedResult<Message>> HistoryAsync()
        {
            return HistoryAsync(new PaginatedRequestParams());
        }

        /// <summary>
        /// Return the message history of the channel
        /// </summary>
        /// <param name="query"><see cref="PaginatedRequestParams"/></param>
        /// <returns><see cref="PaginatedResult{T}"/> of Messages</returns>
        public Task<PaginatedResult<Message>> HistoryAsync(PaginatedRequestParams query)
        {
            query = query ?? new PaginatedRequestParams();

            query.Validate();

            var request = _ablyRest.CreateGetRequest(_basePath + "/messages", Options);

            request.AddQueryParameters(query.GetParameters());

            return _ablyRest.ExecutePaginatedRequest(request, HistoryAsync);
        }

        public void Publish(string name, object data, string clientId = null)
        {
            AsyncHelper.RunSync(() => PublishAsync(name, data, clientId));
        }

        public void Publish(Message message)
        {
            AsyncHelper.RunSync(() => PublishAsync(message));
        }

        public void Publish(IEnumerable<Message> messages)
        {
            AsyncHelper.RunSync(() => PublishAsync(messages));
        }

        public PaginatedResult<Message> History()
        {
            return AsyncHelper.RunSync(HistoryAsync);
        }

        public PaginatedResult<Message> History(PaginatedRequestParams query)
        {
            return AsyncHelper.RunSync(() => HistoryAsync(query));
        }
    }
}
