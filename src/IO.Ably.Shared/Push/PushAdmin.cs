using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Infrastructure;
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
        /// <returns>Updated device.</returns>
        internal async Task<LocalDevice> RegisterDevice(DeviceDetails details)
        {
            // TODO: Add validation.
            // TODO: Add fullwait parameter.
            var request = _restClient.CreateRequest("/push/deviceRegistrations", HttpMethod.Post);
            request.PostData = details;
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

        /// <summary>
        /// Update device recipient information
        /// The public api doesn't expose this method but it's much easier to put it here than to manually call it when needed.
        /// </summary>
        /// <param name="details">Device details which contain the update.</param>
        /// <returns>Updated device.</returns>
        internal async Task PatchDeviceRecipient(DeviceDetails details)
        {
            var body = JObject.FromObject(new
            {
                push = new { recipient = details.Push.Recipient },
            });

            var request = _restClient.CreateRequest($"/push/deviceRegistrations/{details.Id}", new HttpMethod("PATCH"));
            request.PostData = body;
            var result = await _restClient.ExecuteRequest(request);

            // TODO: Throw if result if failed
        }

        /// <summary>
        /// Publish a push notification message.
        /// </summary>
        /// <param name="recipient">Recipient. TODO: When format is know, update to strongly typed object.</param>
        /// <param name="payload">Message payload.</param>
        /// <returns>Task.</returns>
        public async Task PublishAsync(JObject recipient, JObject payload)
        {
            // TODO: Add logging
            var request = _restClient.CreatePostRequest("/push/publish");

            JObject data = new JObject();
            data.Add("recipient", recipient);
            foreach (var property in payload.Properties())
            {
                data.Add(property.Name, property.Value);
            }

            // TODO: Add FullWait to Client options and then to Request
            request.PostData = data;

            var result = _restClient.ExecuteRequest(request);

            // TODO: Throw an exception if fails
        }

        /// <inheritdoc />
        async Task<ChannelSubscription> IPushChannelSubscriptions.SaveAsync(ChannelSubscription subscription)
        {
            // TODO: Add validation
            // TODO: Implement fullWait query param
            var request = _restClient.CreatePostRequest("/push/channelSubscriptions");
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
            // TODO: Add fullwait parameter
            var request = _restClient.CreateRequest("/push/deviceRegistrations/" + details.Id, HttpMethod.Put);
            request.PostData = details;
            var result = await _restClient.ExecuteRequest<DeviceDetails>(request);

            return result;
        }

        /// <inheritdoc />
        async Task<DeviceDetails> IDeviceRegistrations.GetAsync(string deviceId)
        {
            // TODO: Add fullWait parameter
            var request = _restClient.CreateGetRequest($"/push/deviceRegistrations/{deviceId}");

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

            // TODO: Add fullWait parameter
            var request = _restClient.CreateRequest($"/push/deviceRegistrations/{deviceId}", HttpMethod.Delete);

            var result = await _restClient.ExecuteRequest(request);

            // Question: what happens if the request errors
        }
    }
}
