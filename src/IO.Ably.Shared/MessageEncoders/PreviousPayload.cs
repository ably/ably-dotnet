using System;

namespace IO.Ably.MessageEncoders
{
    internal class PreviousPayload : IEquatable<PreviousPayload>
    {
        public bool Equals(PreviousPayload other)
        {
            if (other is null)
            {
                return false;
            }

            return Equals(ByteData, other.ByteData) && StringData == other.StringData && Encoding == other.Encoding;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PreviousPayload) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ByteData != null ? ByteData.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringData != null ? StringData.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Encoding != null ? Encoding.GetHashCode() : 0);
                return hashCode;
            }
        }

        public byte[] ByteData { get; }

        public string StringData { get; }

        public string Encoding { get; }

        public PreviousPayload(string data, string encoding)
        {
            StringData = data;
            Encoding = encoding;
        }

        public PreviousPayload(byte[] data, string encoding)
        {
            ByteData = data;
            Encoding = encoding;
        }
    }
}