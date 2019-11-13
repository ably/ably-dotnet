using IO.Ably.Transport;

namespace IO.Ably
{
    public interface IAblyCodec
    {
        object Decode(object payload, EncodingDecodingContext encodingContext);
        EncodingResult Encode(object payload, EncodingDecodingContext decodingContext);

    }

    public class EncodingResult
    {
        public object NewPayload { get; set; }
        public string NewEncoding { get; set; }
    }

    public class EncodingDecodingContext
    {
        /// <summary>
        /// the base payload of the previous message (ie without any transport-specific encoding step)
        /// </summary>
        public byte[] BaseEncodedPreviousPayload { get; set; }

        /// <summary>
        /// the residual encoding string for this message prior to the present encode/decode step
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// The channel options for the current channel
        /// </summary>
        public ChannelOptions ChannelOptions { get; set; }
    }
}
