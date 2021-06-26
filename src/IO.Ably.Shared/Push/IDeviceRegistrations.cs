using System.Threading.Tasks;

namespace IO.Ably.Push
{
    /// <summary>
    /// Device Registrations APIs. For more information visit the Ably Rest Documentation: https://ably.com/documentation/rest-api#post-device-registration.
    /// </summary>
    public interface IDeviceRegistrations
    {
        /// <summary>
        /// Update a device registration.
        /// </summary>
        /// <param name="details">Device to be updated. Only `clientId`, `metadata` and `Push.Recipient` can be updates. The rest of the values must match what the server contains.
        /// RestApi: https://ably.com/documentation/rest-api#update-device-registration.</param>
        /// <returns>Updated Device Registration.</returns>
        Task<DeviceDetails> SaveAsync(DeviceDetails details); // The Rest API only allows clientId, metadata and push.recipient to be updates FYI: https://ably.com/documentation/rest-api#update-device-registration

        /// <summary>
        /// Obtain the details for a device registered for receiving push registrations.
        /// RestApi: https://ably.com/documentation/rest-api#get-device-registration.
        /// </summary>
        /// <param name="deviceId">Id of the device.</param>
        /// <returns>Returns a DeviceDetails class if the device is found or `null` if not.</returns>
        Task<DeviceDetails> GetAsync(string deviceId);

        /// <summary>
        /// Obtain the details for devices registered for receiving push registrations.
        /// RestApi: https://ably.com/documentation/rest-api#list-device-registrations.
        /// </summary>
        /// <param name="clientId">Optional filter by clientId. DeviceId and ClientId cannot be combined.</param>
        /// <param name="deviceId">Optional filter by deviceId. DeviceId and ClientId cannot be combined.</param>
        /// <param name="limit">Number of results returned. Max allowed value: 1000.</param>
        /// <returns>A paginated list of DeviceDetails.</returns>
        Task<PaginatedResult<DeviceDetails>> List(string clientId = null, string deviceId = null, int? limit = null);

        /// <summary>
        /// Removes a registered device.
        /// RestAPI: https://ably.com/documentation/rest-api#delete-device-registration.
        /// </summary>
        /// <param name="details">DeviceDetails to be removed.</param>
        /// <returns>Task.</returns>
        Task RemoveAsync(DeviceDetails details);

        /// <summary>
        /// Removes a registered device.
        /// RestAPI: https://ably.com/documentation/rest-api#delete-device-registration.
        /// </summary>
        /// <param name="deviceId">The deviceId of the device to be removed.</param>
        /// <returns>Task.</returns>
        Task RemoveAsync(string deviceId);

        // TODO: Discuss with Tom whether to add more helper methods to cover the full Rest Api.
    }
}
