using System;
using Newtonsoft.Json;

namespace Ably
{
    public class MessagePayload
    {
        public const string Base64Encoding = "base64";
        public const string EncryptedEncoding = "cipher+base64";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("encoding", NullValueHandling = NullValueHandling.Ignore)]
        public string Encoding { get; set; }

        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }

        public bool IsEncrypted
        {
            get { return string.Equals(EncryptedEncoding, Encoding, StringComparison.CurrentCultureIgnoreCase); }
        }

        public bool IsBinaryMessage
        {
            get { return string.Equals(Base64Encoding, Encoding, StringComparison.CurrentCultureIgnoreCase); }
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Data: {1}, Type: {2}, Encoding: {3}, Timestamp: {4}, IsEncrypted: {5}, IsBinaryMessage: {6}", Name, Data, Type, Encoding, Timestamp, IsEncrypted, IsBinaryMessage);
        }
    }
}