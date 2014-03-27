using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ably.Realtime
{
    public class AblyRealtime
    {
        /**
	 * The {@link Connection} object for this instance.
	 */
        public Connection Connection { get; private set; }
        private Rest _rest;

        public AblyOptions Options { get { return Rest.Options; } }
        /**
	 * The {@link #Channels} associated with this instance.
	 */
        public ChannelsStore Channels { get; private set; }

        public Rest Rest
        {
            get { return _rest; }
        }

        /**
	 * Instance the Ably library using a key only.
	 * This is simply a convenience constructor for the
	 * simplest case of instancing the library with a key
	 * for basic authentication and no other options.
	 * @param key; String key (obtained from application dashboard)
	 * @throws AblyException
	 */
	public AblyRealtime(string key) : this(new AblyOptions() { Key = key}) {
	}

	/**
	 * Instance the Ably library with the given options.
	 * @param options: see {@link io.ably.types.Options} for options
	 * @throws AblyException
	 */
	public AblyRealtime(AblyOptions options) {
        _rest = new Rest(options);
		Channels = new ChannelsStore(this);
		(Connection = new Connection(this)).Connect();
	}

	/**
	 * Close this instance. This closes the connection.
	 * The connection can be re-opened by calling
	 * {@link Connection#connect}.
	 */
	public void Close() {
		Connection.Close();
	}

	/**
	 * A collection of the Channels associated with this Realtime
	 * instance.
	 *
	 */
	public class ChannelsStore : Dictionary<String, Channel> {
	    private readonly AblyRealtime _realtime;

	    public ChannelsStore(AblyRealtime realtime)
	    {
	        _realtime = realtime;
	    }

	    /**
		 * Get the named channel; if it does not already exist,
		 * create it with default options.
		 * @param channelName the name of the channel
		 * @return the channel
		 */
		public Channel Get(string channelName) {
		    if (ContainsKey(channelName))
		    {
		        return Get(channelName);
		    }

				var channel = new Channel(_realtime, channelName);
				Add(channelName, channel);
			return channel;
		}

		/**
		 * Get the named channel and set the given options, creating it
		 * if it does not already exist.
		 * @param channelName the name of the channel
		 * @param channelOptions the options to set (null to clear options on an existing channel)
		 * @return the channel
		 * @throws AblyException
		 */
		public Channel Get(String channelName, ChannelOptions channelOptions) {
			Channel channel = Get(channelName);
			channel.setOptions(channelOptions);
			return channel;
		}

		public void OnChannelMessage(ITransport transport, ProtocolMessage msg) {
			String channelName = msg.Channel;
			Channel channel;
			channel = _realtime.Channels.Get(channelName); 
			if(channel == null) {
				Logger.Current.Debug("Received channel message for non-existent channel");
				return;
			}
			channel.OnChannelMessage(msg);
		}
	}

	/********************
	 * internal
	 ********************/
    }


    public interface IConnectionStateListener
    {
        void OnConnectionStateChanged(ConnectionStateChange state);
    }

    public class ConnectionStateChange
    {
        public ConnectionState previous;
        public ConnectionState current;
        public long retryIn;
        public ErrorInfo reason;

        public ConnectionStateChange(ConnectionState previous, ConnectionState current, long retryIn, ErrorInfo reason)
        {
            this.previous = previous;
            this.current = current;
            this.retryIn = retryIn;
            this.reason = reason;
        }
    }

    public class ConnetionStateMulticaster : List<IConnectionStateListener>, IConnectionStateListener
    {
        public void OnConnectionStateChanged(ConnectionStateChange state)
        {
            foreach (var item in this)
            {
                try
                {
                    item.OnConnectionStateChanged(state);
                }
                catch (Exception ex)
                {
                    Logger.Current.Debug("Error notifying member.");
                }
            }
        }
    }



    /**
     * A class representing the connection associated with an AblyRealtime instance.
     * The Connection object exposes the lifecycle and parameters of the realtime connection.
     */
    public enum ConnectionState
    {
        Initialized,
        Connecting,
        Connected,
        Disconnected,
        Suspended,
        Closed,
        Failed
    }

    public class Connection : IConnectionStateListener
    {

        /**
         * Connection states. See Ably Realtime API documentation for more details.
         */


        /**
         * The collection of state change listeners. Listeners can register
         * and deregister for state change events with
         * {@link io.ably.realtime.ConnectionStateListener.Multicaster#add}
         * and {@link io.ably.realtime.ConnectionStateListener.Multicaster#remove}
         */
        private readonly ConnetionStateMulticaster _listeners;

        /**
         * The current state of this Connection.
         */
        public ConnectionState State { get; set; }

        /**
         * Error information associated with a connection failure.
         */
        public ErrorInfo Reason { get; set; }

        /**
         * The assigned connection id.
         */
        public string Id { get; set; }

        /**
         * The serial number of the last message to be received on this connection.
         */
        public long Serial { get; set; }

        public ConnetionStateMulticaster Listeners
        {
            get { return _listeners; }
        }

        /**
         * Causes the library to re-attempt connection, if it was previously explicitly
         * closed by the user, or was closed as a result of an unrecoverable error.
         */
        public void Connect()
        {
            _connectionManager.RequestState(ConnectionState.Connecting);
        }

        /**
         * Causes the connection to close, entering the closed state, from any state except
         * the failed state. Once closed, the library will not attempt to re-establish the
         * connection without a call to {@link #connect}.
         */
        public void Close()
        {
            Id = null;
            _connectionManager.RequestState(ConnectionState.Closed);
        }

        /*****************
         * internal
         *****************/

        public Connection(AblyRealtime ably)
        {
            _ably = ably;
            _listeners = new ConnetionStateMulticaster();
            _connectionManager = new ConnectionManager(ably, this);
        }

        public void OnConnectionStateChanged(ConnectionStateChange stateChange)
        {
            State = stateChange.current;
            Reason = stateChange.reason;
            _listeners.OnConnectionStateChanged(stateChange);
        }

        private readonly AblyRealtime _ably;
        private readonly ConnectionManager _connectionManager;
        public ConnectionManager ConnectionManager { get { return _connectionManager; }}
    }

}
