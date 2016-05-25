using IO.Ably.Encryption;
using IO.Ably.Platform;

namespace IO.Ably.Rest
{
    public class ChannelOptions
    {
        protected bool Equals(ChannelOptions other)
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

        public bool Encrypted { get; private set; }
        public CipherParams CipherParams { get; private set; }

        public ChannelOptions(bool encrypted = false, CipherParams @params = null)
        {
            Encrypted = encrypted;
            CipherParams = @params ?? Crypto.GetDefaultParams();
        }

        public ChannelOptions(CipherParams @params)
        {
            Encrypted = true;
            CipherParams = @params;
        }
    }
}