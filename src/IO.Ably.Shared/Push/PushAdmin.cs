using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// Push Admin APIs.
    /// </summary>
    public class PushAdmin : IPushChannelSubscriptions, IDeviceRegistrations
    {
        private readonly AblyRest _restClient;

        private ClientOptions Options => _restClient.Options;

        private readonly ILogger _logger;

        internal PushAdmin(AblyRest restClient, ILogger logger)
        {
            _restClient = restClient;
            _logger = logger;
        }

        /// <summary>
        /// Exposes channel subscriptions apis.
        /// </summary>
        public IPushChannelSubscriptions ChannelSubscriptions => this;

        /// <summary>
        /// Exposes device registrations apis.
        /// </summary>
        public IDeviceRegistrations DeviceRegistrations => this;

        /// <summary>
        /// Register a new device
        /// The public api doesn't expose this method but it's much easier to put it here than to manually call it when needed.
        /// </summary>
        /// <param name="details">Device details needed for registration.</param>
        /// <returns>Updated device including a deviceIdentityToken assigned by the Push service.</returns>
        internal async Task<LocalDevice> RegisterDevice(DeviceDetails details)
        {
            ValidateDeviceDetails();
            var request = _restClient.CreateRequest("/push/deviceRegistrations/", HttpMethod.Post);
            AddFullWaitIfNecessary(request);
            request.PostData = details;

            return await ExecuteRequest();

            void ValidateDeviceDetails()
            {
                if (details is null)
                {
                    throw new AblyException("DeviceDetails is null.", ErrorCodes.BadRequest);
                }

                if (details.Id.IsEmpty())
                {
                    throw new AblyException("DeviceDetails needs an non empty Id parameter.", ErrorCodes.BadRequest);
                }

                if (details.Platform.IsEmpty())
                {
                    throw new AblyException("DeviceDetails needs a valid Platform. Supported values are 'ios', 'android' or 'browser'.", ErrorCodes.BadRequest);
                }

                if (details.FormFactor.IsEmpty())
                {
                    throw new AblyException(
                        "DeviceDetails needs a valid FormFactor. Supporter values are 'phone', 'tablet', 'desktop', 'tv', 'watch', 'car' or 'embedded'.", ErrorCodes.BadRequest);
                }

                if (details.Push?.Recipient is null)
                {
                    throw new AblyException("A valid recipient is required to register a device.", ErrorCodes.BadRequest);
                }
            }

            async Task<LocalDevice> ExecuteRequest()
            {
                try
                {
                    var response = await _restClient.ExecuteRequest(request);

                    var jsonResponse = JObject.Parse(response.TextResponse);
                    var localDevice = jsonResponse.ToObject<LocalDevice>();
                    var deviceToken = (string)jsonResponse["deviceIdentityToken"]?["token"];
                    if (deviceToken != null)
                    {
                        localDevice.DeviceIdentityToken = deviceToken;
                    }

                    return localDevice;
                }
                catch (JsonReaderException jsonEx)
                {
                    _logger.Error("Error registering device. Invalid response", jsonEx);
                    var error = new ErrorInfo("Error registering device. Invalid response.", ErrorCodes.InternalError);
                    throw new AblyException(error, jsonEx);
                }
                catch (AblyException e)
                {
                    _logger.Error("Error registering Device", e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update device recipient information
        /// The public api doesn't expose this method but it's much easier to put it here than to manually call it when needed.
        /// </summary>
        /// <param name="details">Device details which contain the update.</param>
        internal async Task PatchDeviceRecipient(DeviceDetails details)
        {
            var body = JObject.FromObject(new
            {
                push = new { recipient = details.Push.Recipient },
            });

            ValidateDeviceDetails();
            var request = _restClient.CreateRequest($"/push/deviceRegistrations/{details.Id}", new HttpMethod("PATCH"));
            AddFullWaitIfNecessary(request);
            request.PostData = body;
            _ = await _restClient.ExecuteRequest(request);

            void ValidateDeviceDetails()
            {
                if (details is null)
                {
                    throw new AblyException("DeviceDetails is null.", ErrorCodes.BadRequest);
                }

                if (details.Id.IsEmpty())
                {
                    throw new AblyException("DeviceDetails needs an non empty Id parameter.", ErrorCodes.BadRequest);
                }

                if (details.Push?.Recipient is null)
                {
                    throw new AblyException("A valid recipient is required to patch device recipient.", ErrorCodes.BadRequest);
                }
            }
        }

        /// <summary>
        /// Publish a push notification message.
        /// </summary>
        /// <param name="recipient">Recipient. TODO: When format is know, update to strongly typed object.</param>
        /// <param name="payload">Message payload.</param>
        /// <returns>Task.</returns>
        public async Task PublishAsync(JObject recipient, JObject payload)
        {
            ValidateRequest();
            var request = _restClient.CreatePostRequest("/push/publish");
            AddFullWaitIfNecessary(request);
            JObject data = new JObject();
            data.Add("recipient", recipient);
            foreach (var property in payload.Properties())
            {
                data.Add(property.Name, property.Value);
            }

            request.PostData = data;

            _ = _restClient.ExecuteRequest(request);

            void ValidateRequest()
            {
                if (recipient is null)
                {
                    throw new AblyException("Please provide a valid and non-empty recipient", ErrorCodes.BadRequest);
                }

                if (payload is null)
                {
                    throw new AblyException("Please provide a non-empty payload", ErrorCodes.BadRequest);
                }
            }
        }

        /// <inheritdoc />
        async Task<ChannelSubscription> IPushChannelSubscriptions.SaveAsync(ChannelSubscription subscription)
        {
            // TODO: Add validation
            var request = _restClient.CreatePostRequest("/push/channelSubscriptions");
            AddFullWaitIfNecessary(request);
            request.PostData = subscription;

            return await _restClient.ExecuteRequest<ChannelSubscription>(request);
        }

        /// <inheritdoc />
        async Task<PaginatedResult<ChannelSubscription>> IPushChannelSubscriptions.ListAsync(string channel, string clientId, string deviceId, int? limit) // TODO: Update parametrs to PaginatedQuery
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (channel.IsNotEmpty())
            {
                queryParams.Add("channel", channel);
            }

            if (clientId.IsNotEmpty())
            {
                queryParams.Add("clientId", clientId);
            }

            if (deviceId.IsNotEmpty())
            {
                queryParams.Add("deviceId", deviceId);
            }

            if (limit.HasValue)
            {
                queryParams.Add("limit", limit.ToString());
            }

            var url = "/push/channelSubscriptions";
            if (queryParams.Any())
            {
                url += "?" + queryParams.ToQueryString();
            }

            // TODO: Use paginated query
            var request = _restClient.CreateGetRequest(url);

            return await _restClient.ExecuteRequest<PaginatedResult<ChannelSubscription>>(request);
        }

        /// <inheritdoc />
        async Task IPushChannelSubscriptions.RemoveAsync(ChannelSubscription subscription) // TODO: Do we allow to specify the channel as well.
        {
            // TODO: Validation
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (subscription.ClientId.IsNotEmpty())
            {
                queryParams.Add("clientId", subscription.ClientId);
            }

            if (subscription.DeviceId.IsNotEmpty())
            {
                queryParams.Add("deviceId", subscription.DeviceId);
            }

            var url = "/push/channelSubscriptions";
            if (queryParams.Any())
            {
                url += "?" + queryParams.ToQueryString();
            }

            var request = _restClient.CreateRequest(url, HttpMethod.Delete);
            AddFullWaitIfNecessary(request);
            _ = await _restClient.ExecuteHttpRequest(request);

            // TODO: Handle errors
        }

        /// <inheritdoc />
        async Task<PaginatedResult<string>> IPushChannelSubscriptions.ListChannelsAsync()
        {
            var request = _restClient.CreateGetRequest("/push/channels");

            // TODO: Convert to proper paginated request
            return await _restClient.ExecuteRequest<PaginatedResult<string>>(request);
        }

        /// <inheritdoc />
        async Task<DeviceDetails> IDeviceRegistrations.SaveAsync(DeviceDetails details)
        {
            var request = _restClient.CreateRequest("/push/deviceRegistrations/" + details.Id, HttpMethod.Put);
            AddFullWaitIfNecessary(request);
            request.PostData = details;
            var result = await _restClient.ExecuteRequest<DeviceDetails>(request);

            return result;
        }

        /// <inheritdoc />
        async Task<DeviceDetails> IDeviceRegistrations.GetAsync(string deviceId)
        {
            var request = _restClient.CreateGetRequest($"/push/deviceRegistrations/{deviceId}");
            AddFullWaitIfNecessary(request);

            return await _restClient.ExecuteRequest<DeviceDetails>(request);
        }

        /// <inheritdoc />
        async Task<PaginatedResult<DeviceDetails>> IDeviceRegistrations.List(string clientId, string deviceId, int? limit)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (clientId.IsNotEmpty())
            {
                queryParams.Add("clientId", clientId);
            }

            if (deviceId.IsNotEmpty())
            {
                queryParams.Add("deviceId", deviceId);
            }

            if (limit.HasValue)
            {
                queryParams.Add("limit", limit.ToString());
            }

            var url = "/push/deviceRegistrations";
            if (queryParams.Any())
            {
                url += "?" + queryParams.ToQueryString();
            }

            var request = _restClient.CreateGetRequest(url);

            // TODO: Replace with proper paginated request
            return await _restClient.ExecuteRequest<PaginatedResult<DeviceDetails>>(request);
        }

        /// <inheritdoc />
        async Task IDeviceRegistrations.RemoveAsync(DeviceDetails details)
        {
            await ((IDeviceRegistrations)this).RemoveAsync(details.Id);
        }

        /// <inheritdoc />
        async Task IDeviceRegistrations.RemoveAsync(string deviceId)
        {
            // TODO: Validate the deviceId is not empty
            var request = _restClient.CreateRequest($"/push/deviceRegistrations/{deviceId}", HttpMethod.Delete);
            AddFullWaitIfNecessary(request);
            var result = await _restClient.ExecuteRequest(request);

            // Question: what happens if the request errors
        }

        private void AddFullWaitIfNecessary(AblyRequest request)
        {
            if (Options.PushAdminFullWait)
            {
                request.AddQueryParameters(new[] { new KeyValuePair<string, string>("fullWait", "true") });
            }
        }
    }
}
