using IO.Ably.Encryption;

namespace IO.Ably
{
    public class CipherParams
    {
        protected bool Equals(CipherParams other)
        {
            return string.Equals(Algorithm, other.Algorithm) && Equals(Key, other.Key) && Equals(Iv, other.Iv) && Mode == other.Mode && KeyLength == other.KeyLength;
        }

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

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((CipherParams)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Algorithm?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Iv != null ? Iv.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Mode;
                hashCode = (hashCode * 397) ^ KeyLength;
                return hashCode;
            }
        }

        public string Algorithm { get; }

        public byte[] Key { get; }

        public byte[] Iv { get; }
        public CipherMode Mode { get; }
        public int KeyLength => Key?.Length * 8 ?? 0;

        public string CipherType
        {
            get { return $"{Algorithm}-{KeyLength}-{Mode}"; }
        }

        public CipherParams(byte[] key)
            : this(Crypto.DefaultAlgorithm, key)
        {

        }

        public CipherParams(string algorithm, byte[] key, CipherMode? mode = null, byte[] iv = null)
        {
            Algorithm = algorithm.IsEmpty() ? Crypto.DefaultAlgorithm : algorithm;
            Key = key;
            Mode = mode ?? Crypto.DefaultMode;
            Iv = iv;
        }

        public CipherParams(string algorithm, string base64Key = null, CipherMode mode = Crypto.DefaultMode, string base64Iv = null)
        {
            Algorithm = algorithm.IsEmpty() ? Crypto.DefaultAlgorithm : algorithm;
            Key = base64Key.FromBase64();
            Mode = mode;
            Iv = base64Iv.FromBase64();
        }
    }
}