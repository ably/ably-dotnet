using System.Threading.Tasks;

namespace IO.Ably.Push
{
    /// <summary>
    /// Apis for managing ChannelSubscriptions.
    /// </summary>
    public interface IPushChannelSubscriptions
    {
        /// <summary>
        /// Subscribe either a single device or all devices associated with a client ID to receive push notifications from messages sent to a channel.
        /// RestApi: https://ably.com/documentation/rest-api#post-channel-subscription.
        /// </summary>
        /// <param name="subscription">Channel subscription object.</param>
        /// <returns>Return an updated Channel subscription object.</returns>
        Task<ChannelSubscription> SaveAsync(ChannelSubscription subscription);

        /// <summary>
        /// Get a list of push notification subscriptions to channels.
        /// RestApi: https://ably.com/documentation/rest-api#list-channel-subscriptions.
        /// </summary>
        /// <param name="channel">Filter by channel name.</param>
        /// <param name="clientId">Filter to restrict to subscriptions associated with that clientId. Cannot be used together with deviceId.</param>
        /// <param name="deviceId">Filter to restrict to subscriptions associated with that deviceId. Cannot be used together with clientId.</param>
        /// <param name="limit">Number of returns to return. Max limit is 1000.</param>
        /// <returns>Returns a paginated list of ChannelSubscriptions.</returns>
        Task<PaginatedResult<ChannelSubscription>> ListAsync(string channel, string clientId = null, string deviceId = null, int? limit = null); // TODO: Add request object

        /// <summary>
        /// Stop receiving push notifications when push messages are published on the specified channels.
        /// Please note that this operation is done asynchronously so immediate requests subsequent to this delete request may briefly still return the subscription.
        /// RestApi: https://ably.com/documentation/rest-api#delete-channel-subscription.
        /// </summary>
        /// <param name="subscription">Channel Subscription object to unsubscribe.</param>
        /// <returns>Task.</returns>
        Task RemoveAsync(ChannelSubscription subscription); // TODO: Add request object and allow other params

        /// <summary>
        /// List all channels with at least one subscribed device.
        /// RestApi: https://ably.com/documentation/rest-api#list-channels.
        /// </summary>
        /// <returns>Paginated list of channel names.</returns>
        Task<PaginatedResult<string>> ListChannelsAsync(); // TODO: Create a return type that is not string. Note: Java implementation has possible deviceId parameter but the REST documentation doesn't include it.
    }
}
