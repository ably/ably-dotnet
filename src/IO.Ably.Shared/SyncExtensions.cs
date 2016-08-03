using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Rest;

namespace IO.Ably.SyncExtensions
{
    public static class SyncExtensions
    {
        public static void Publish(this IRestChannel restChannel, string name, object data, string clientId = null)
        {
            AsyncHelper.RunSync(() => restChannel.PublishAsync(name, data, clientId));
        }

        public static void Publish(this IRestChannel restChannel, Message message)
        {
            AsyncHelper.RunSync(() => restChannel.PublishAsync(message));
        }

        public static void Publish(this IRestChannel restChannel, IEnumerable<Message> messages)
        {
            AsyncHelper.RunSync(() => restChannel.PublishAsync(messages));
        }

        public static PaginatedResult<Message> History(this IRestChannel restChannel)
        {
            return AsyncHelper.RunSync(restChannel.HistoryAsync);
        }

        public static PaginatedResult<Message> History(this IRestChannel restChannel, HistoryRequestParams query)
        {
            return AsyncHelper.RunSync(() => restChannel.HistoryAsync(query));
        }

        public static PaginatedResult<T> Next<T>(this PaginatedResult<T> paginatedResult) where T : class
        {
            return AsyncHelper.RunSync(paginatedResult.NextAsync);
        }

        public static PaginatedResult<T> First<T>(this PaginatedResult<T> paginatedResult) where T : class
        {
            return AsyncHelper.RunSync(paginatedResult.FirstAsync);
        }

        public static PaginatedResult<Stats> Stats(this IStatsCommands statsCommands)
        {
            return AsyncHelper.RunSync(statsCommands.StatsAsync);
        }

        public static PaginatedResult<Stats> Stats(this IStatsCommands statsCommands, StatsRequestParams query)
        {
            return AsyncHelper.RunSync(() => statsCommands.StatsAsync(query));
        }

        public static DateTimeOffset Time(this IRestClient restClient)
        {
            return AsyncHelper.RunSync(restClient.TimeAsync);
        }

        public static TokenDetails RequestToken(this IAblyAuth auth, TokenParams tokenParams = null,
            AuthOptions options = null)
        {
            return AsyncHelper.RunSync(() => auth.RequestTokenAsync(tokenParams, options));
        }

        public static TokenDetails Authorise(this IAblyAuth auth, TokenParams tokenParams = null,
            AuthOptions options = null)
        {
            return AsyncHelper.RunSync(() => auth.AuthoriseAsync(tokenParams, options));
        }

        public static TokenRequest CreateTokenRequest(this IAblyAuth auth, TokenParams tokenParams = null,
            AuthOptions authOptions = null)
        {
            return AsyncHelper.RunSync(() => auth.CreateTokenRequestAsync(tokenParams, authOptions));
        }
    }

    internal static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new
          TaskFactory(CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _myTaskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            AsyncHelper._myTaskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}
