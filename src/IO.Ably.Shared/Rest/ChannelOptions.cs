using IO.Ably;
using IO.Ably.Encryption;

namespace IO.Ably
{
    /// <summary>
    /// Channel options used for initialising channels.
    /// </summary>
    public class ChannelOptions
    {
        internal ILogger Logger { get; set; }

        /// <summary>
        /// Indicates whether the channel is encrypted.
        /// </summary>
        public bool Encrypted { get; private set; }

        /// <summary>
        /// If Encrypted it provides the <see cref="IO.Ably.CipherParams"/>.
        /// </summary>
        public CipherParams CipherParams { get; private set; }

        public ChannelParams Params = new ChannelParams();

        public ChannelModes Modes = new ChannelModes();

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
}
