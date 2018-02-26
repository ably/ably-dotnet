using IO.Ably.Encryption;
using IO.Ably;

namespace IO.Ably
{
    public class ChannelOptions
    {
        private bool Equals(ChannelOptions other)
        {
            return Encrypted == other.Encrypted && Equals(CipherParams, other.CipherParams);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ChannelOptions) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Encrypted.GetHashCode()*397) ^ (CipherParams?.GetHashCode() ?? 0);
            }
        }

        internal ILogger Logger { get; set; }
        public bool Encrypted { get; private set; }
        public CipherParams CipherParams { get; private set; }

        public ChannelOptions(CipherParams @params) : this(IO.Ably.DefaultLogger.LoggerInstance, true, @params) {}
        public ChannelOptions(bool encrypted = false, CipherParams @params = null) : this(null, encrypted, @params) { }
        internal ChannelOptions(ILogger logger, bool encrypted = false, CipherParams @params = null)
        {
            Logger = logger ?? IO.Ably.DefaultLogger.LoggerInstance;
            Encrypted = encrypted;
            CipherParams = @params ?? Crypto.GetDefaultParams();
        }

        public ChannelOptions(byte[] key)
        {
            Logger = IO.Ably.DefaultLogger.LoggerInstance;
            Encrypted = true;
            CipherParams = new CipherParams(key);
        }
    }
}