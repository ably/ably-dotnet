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
        Task<PushChannelSubscription> SaveAsync(PushChannelSubscription subscription);

        /// <summary>
        /// Get a list of push notification subscriptions to channels.
        /// RestApi: https://ably.com/documentation/rest-api#list-channel-subscriptions.
        /// </summary>
        /// <param name="requestFilter">Provides a way to filter results. <see cref="ListSubscriptionsRequest"/>.</param>
        /// <returns>Returns a paginated list of ChannelSubscriptions.</returns>
        Task<PaginatedResult<PushChannelSubscription>> ListAsync(ListSubscriptionsRequest requestFilter);

        /// <summary>
        /// Stop receiving push notifications when push messages are published on the specified channels.
        /// Please note that this operation is done asynchronously so immediate requests subsequent to this delete request may briefly still return the subscription.
        /// RestApi: https://ably.com/documentation/rest-api#delete-channel-subscription.
        /// </summary>
        /// <param name="subscription">Channel Subscription object to unsubscribe.</param>
        /// <returns>Task.</returns>
        Task RemoveAsync(PushChannelSubscription subscription); // TODO: Add request object and allow other params

        /// <summary>
        /// List all channels with at least one subscribed device.
        /// RestApi: https://ably.com/documentation/rest-api#list-channels.
        /// </summary>
        /// <param name="requestParams">Allows adding a limit to the number of results and supports paginated requests handling.</param>
        /// <returns>Paginated list of channel names.</returns>
        Task<PaginatedResult<string>> ListChannelsAsync(PaginatedRequestParams requestParams); // TODO: Create a return type that is not string. Note: Java implementation has possible deviceId parameter but the REST documentation doesn't include it.
    }
}
