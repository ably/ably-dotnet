using System;
using System.ComponentModel.Design;
using System.Threading.Channels;
using IO.Ably;
using IO.Ably.Encryption;

namespace IO.Ably
{
    /// <summary>
    /// Channel options used for initialising channels.
    /// </summary>
    public class ChannelOptions
    {
        private ChannelModes _modes = new ChannelModes();
        private ChannelParams _params = new ChannelParams();

        internal ILogger Logger { get; set; }

        /// <summary>
        /// Indicates whether the channel is encrypted.
        /// </summary>
        public bool Encrypted { get; private set; }

        /// <summary>
        /// If Encrypted it provides the <see cref="IO.Ably.CipherParams"/>.
        /// </summary>
        public CipherParams CipherParams { get; private set; }

        /// <summary>
        /// Params allows custom parameters to be passed to the server when attaching the channel.
        /// In that list are 'delta' and 'rewind'. For more information about channel params visit
        /// https://www.ably.io/documentation/realtime/channel-params.
        /// </summary>
        public ChannelParams Params
        {
            get => _params;
            set => _params = value ?? new ChannelParams();
        }

        /// <summary>
        /// Channel Modes like Params are passed to the server when attaching the channel.
        /// They let specify how the channel will behave and what is allowed. <see cref="ChannelMode"/>.
        /// </summary>
        public ChannelModes Modes
        {
            get => _modes;
            set => _modes = value ?? new ChannelModes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelOptions"/> class.
        /// </summary>
        /// <param name="params"><see cref="IO.Ably.CipherParams"/>.</param>
        public ChannelOptions(CipherParams @params)
            : this(DefaultLogger.LoggerInstance, true, @params) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelOptions"/> class.
        /// </summary>
        /// <param name="encrypted">whether it is encrypted.</param>
        /// <param name="params">optional, <see cref="IO.Ably.CipherParams"/>.</param>
        public ChannelOptions(bool encrypted = false, CipherParams @params = null)
            : this(null, encrypted, @params) { }

        internal ChannelOptions(ILogger logger, bool encrypted = false, CipherParams @params = null)
        {
            Logger = logger ?? DefaultLogger.LoggerInstance;
            Encrypted = encrypted;
            CipherParams = @params ?? Crypto.GetDefaultParams();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelOptions"/> class.
        /// </summary>
        /// <param name="key">cipher key.</param>
        public ChannelOptions(byte[] key)
        {
            Logger = DefaultLogger.LoggerInstance;
            Encrypted = true;
            CipherParams = new CipherParams(key);
        }

        private bool Equals(ChannelOptions other)
        {
            return Encrypted == other.Encrypted && Equals(CipherParams, other.CipherParams);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ChannelOptions)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Encrypted.GetHashCode() * 397) ^ (CipherParams?.GetHashCode() ?? 0);
            }
        }
    }

    /// <summary>
    /// Helper methods to make adding channel options easier.
    /// </summary>
    public static class ChannelOptionsExtensions
    {
        /// <summary>
        /// Makes the ChannelOptions API easier to use by providing this convenience method to add ChannelModes.
        /// You can do `new ChannelOptions().WithModes(ChannelMode.Publish, ChannelMode.Subscribe)`.
        /// </summary>
        /// <param name="options">the <see cref="ChannelOptions"/> that will be modified.</param>
        /// <param name="modes">the <see cref="ChannelMode"/> list that will be added to ChannelOptions.</param>
        /// <returns>the same ChannelOptions object so other calls can be chained.</returns>
        public static ChannelOptions WithModes(this ChannelOptions options, params ChannelMode[] modes)
        {
            foreach (var mode in modes)
            {
                options.Modes.Add(mode);
            }

            return options;
        }

        /// <summary>
        /// Makes the ChannelOptions API easier to use by providing this convenience method to add ChannelParams.
        /// You can do `new ChannelOptions().WithParam("key", "value")`.
        /// </summary>
        /// <param name="options">the <see cref="ChannelOptions"/> that will be modified.</param>
        /// <param name="key">the channel param key.</param>
        /// <param name="value">the channel param value.</param>
        /// <returns>the same ChannelOptions object so other calls can be chained.</returns>
        public static ChannelOptions WithParam(this ChannelOptions options, string key, string value)
        {
            options.Params[key] = value;
            return options;
        }

        /// <summary>
        /// Makes adding Rewind by a number of messages easier.
        /// Full documentation can be found here: https://www.ably.io/documentation/realtime/channel-params#rewind.
        /// </summary>
        /// <param name="options">the <see cref="ChannelOptions"/> that will be modified.</param>
        /// <param name="numberOfMessages">the value passed to Rewind param.</param>
        /// <returns>the same ChannelOptions object so other calls can be chained.</returns>
        public static ChannelOptions WithRewind(this ChannelOptions options, int numberOfMessages)
        {
            return options.WithParam("rewind", numberOfMessages.ToString());
        }

        /// <summary>
        /// Makes adding Rewind by a time period with an option interval easier.
        /// Full documentation can be found here: https://www.ably.io/documentation/realtime/channel-params#rewind.
        /// </summary>
        /// <param name="options">the <see cref="ChannelOptions"/> that will be modified.</param>
        /// <param name="rewindBy">by how much should we rewind.</param>
        /// <param name="limit">the max number of messages that should be returned. Max: 100.</param>
        /// <returns>the same ChannelOptions object so other calls can be chained.</returns>
        public static ChannelOptions WithRewind(this ChannelOptions options, TimeSpan rewindBy, int? limit)
        {
            var result = options.WithParam("rewind", rewindBy.TotalSeconds + "s");
            if (limit.HasValue && limit > 0)
            {
                var l = Math.Max(limit.Value, 100);
                result.WithParam("rewindLimit", l.ToString());
            }

            return result;
        }
    }
}
