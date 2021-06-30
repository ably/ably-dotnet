using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Infrastructure;
using IO.Ably.Realtime.Workflow;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Realtime
{
    /// <summary>
    /// Manages Realtime channels.
    /// </summary>
    public class RealtimeChannels : IChannels<IRealtimeChannel>
    {
        internal ILogger Logger { get; }

        private ConcurrentDictionary<string, RealtimeChannel> Channels { get; } = new ConcurrentDictionary<string, RealtimeChannel>();

        private readonly AblyRealtime _realtimeClient;

        internal RealtimeChannels(AblyRealtime realtimeClient, Connection connection)
        {
            _realtimeClient = realtimeClient;
            Logger = realtimeClient.Logger;
            connection.InternalStateChanged += ConnectionStateChange;
        }

        private void ConnectionStateChange(object sender, ConnectionStateChange stateChange)
        {
            foreach (var channel in Channels.Values)
            {
                try
                {
                    channel.ConnectionStateChanged(stateChange);
                }
                catch (Exception e)
                {
                    // TODO: Send to Sentry
                    Logger.Error($"Error notifying channel '{channel.Name}' of connection stage change", e);
                }
            }
        }

        /// <inheritdoc/>
        public IRealtimeChannel Get(string name)
        {
            return Get(name, null);
        }

        /// <inheritdoc/>
        public IRealtimeChannel Get(string name, ChannelOptions options)
        {
            // if the channel cannot be found
            if (!Channels.TryGetValue(name, out var result))
            {
                // create a new instance using the passed in option
                var channel = new RealtimeChannel(name, _realtimeClient.Options.GetClientId(), _realtimeClient, options);
                result = Channels.AddOrUpdate(name, channel, (s, realtimeChannel) =>
                {
                    if (options != null)
                    {
                        realtimeChannel.Options = options;
                    }

                    return realtimeChannel;
                });
            }
            else
            {
                if (options != null)
                {
                    if (result.ShouldReAttach(options))
                    {
                        throw new AblyException(new ErrorInfo("Channels.Get() cannot be used to set channel options that would cause the channel to reattach. Please, use Channel.SetOptions() instead.", 40000, HttpStatusCode.BadRequest));
                    }

                    result.SetOptions(options);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public IRealtimeChannel this[string name] => Get(name);

        /// <inheritdoc/>
        public bool Release(string name)
        {
            if (Logger.IsDebug) { Logger.Debug($"Releasing channel #{name}"); }
            RealtimeChannel channel = null;
            if (Channels.TryGetValue(name, out channel))
            {
                EventHandler<ChannelStateChange> eventHandler = null;
                eventHandler = (s, args) =>
                {
                    var detachedChannel = (RealtimeChannel)s;
                    if (args.Current == ChannelState.Detached || args.Current == ChannelState.Failed)
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug($"Channel #{name} was removed from Channel list. State {args.Current}");
                        }

                        detachedChannel.InternalStateChanged -= eventHandler;

                        RealtimeChannel removedChannel;
                        if (Channels.TryRemove(name, out removedChannel))
                        {
                            removedChannel.RemoveAllListeners();
                        }
                    }
                    else
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug($"Waiting to remove Channel #{name}. State {args.Current}");
                        }
                    }
                };

                channel.InternalStateChanged += eventHandler;
                channel.Detach();
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void ReleaseAll()
        {
            var channelList = Channels.Keys.ToArray();
            foreach (var channelName in channelList)
            {
                Release(channelName);
            }
        }

        internal void CleanupChannels()
        {
            try
            {
                var channels = Channels.Keys.ToList();
                foreach (var channelName in channels)
                {
                    var success = Channels.TryRemove(channelName, out RealtimeChannel channel);
                    if (success)
                    {
                        channel.RemoveAllListeners();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error while disposing channels", e);
            }
        }

        /// <inheritdoc/>
        public bool Exists(string name)
        {
            return Channels.ContainsKey(name);
        }

        /// <inheritdoc/>
        IEnumerator<IRealtimeChannel> IEnumerable<IRealtimeChannel>.GetEnumerator()
        {
            return Channels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Channels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        internal JArray GetCurrentState()
        {
            return new JArray(Channels.Values.Select(x => x.GetCurrentState()));
        }

        internal Task ExecuteCommand(ChannelCommand cmd)
        {
            var channelName = cmd.ChannelName;
            var affectedChannels = Channels.Values
                                        .ToArray()
                                        .Where(x => cmd.ChannelName.IsEmpty() || x.Name.EqualsTo(channelName))
                                        .ToList();

            foreach (var channel in affectedChannels)
            {
                switch (cmd.Command)
                {
                    case InitialiseFailedChannelsOnConnect _:
                        HandleInitialiseFailedChannelsCommand(channel);
                        break;
                    default:
                        Logger.Debug($"Channels can't handle command: '{cmd.Name}'");
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private void HandleInitialiseFailedChannelsCommand(RealtimeChannel channel)
        {
            switch (_realtimeClient.Connection.State)
            {
                case ConnectionState.Closed:
                case ConnectionState.Failed:
                    /* (RTN11d)
                     * If the [Connection] state is FAILED,
                     * transitions all the channels to INITIALIZED */
                    channel.SetChannelState(ChannelState.Initialized);
                    break;
            }
        }
    }
}
