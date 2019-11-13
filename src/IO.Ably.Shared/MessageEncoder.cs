using IO.Ably.Transport;

namespace IO.Ably
{
    /// <summary>
    /// Interface for implementing generic codecs that work alongside Ably build in ones.
    /// To start with the main implementation will be the Delta codec which supports the vcdiff encoding.
    /// </summary>
    public interface IAblyCodec
    {
        /// <summary>
        /// Decodes a payload.
        /// </summary>
        /// <param name="payload">the payload being decoded.</param>
        /// <param name="decodingContext">the context needed for the operation.</param>
        /// <returns>a decoded object.</returns>
        object Decode(object payload, EncodingDecodingContext decodingContext);

        /// <summary>
        /// Encodes a payload.
        /// </summary>
        /// <param name="payload">the payload that needs to be encoded.</param>
        /// <param name="encodingContext">the context needed for the operation.</param>
        /// <returns><see cref="EncodingResult"/>.</returns>
        EncodingResult Encode(object payload, EncodingDecodingContext encodingContext);
    }

    /// <summary>
    /// Result from an encoding operation.
    /// </summary>
    public class EncodingResult
    {
        /// <summary>
        /// The encoded payload.
        /// </summary>
        public object NewPayload { get; set; }

        /// <summary>
        /// The encoding string after the payload was encoded.
        /// </summary>
        public string NewEncoding { get; set; }
    }

    /// <summary>
    /// Class used to provide context between different encoders.
    /// </summary>
    public class EncodingDecodingContext
    {
        /// <summary>
        /// the base payload of the previous message (ie without any transport-specific encoding step).
        /// </summary>
        public object BaseEncodedPreviousPayload { get; set; }

        /// <summary>
        /// the residual encoding string for this message prior to the present encode/decode step.
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// The channel options for the current channel.
        /// </summary>
        public ChannelOptions ChannelOptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingDecodingContext"/> class.
        /// </summary>
        public EncodingDecodingContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingDecodingContext"/> class.
        /// </summary>
        /// <param name="options">Channel options used for the encode / decode operations.</param>
        public EncodingDecodingContext(ChannelOptions options)
        {
            ChannelOptions = options;
        }
    }

    /// <summary>
    /// Helpers methods for <see cref="EncodingDecodingContext"/>.
    /// </summary>
    public static class EncodingDecodingContextHelpers
    {
        /// <summary>
        /// Creates a new <see cref="EncodingDecodingContext"/> from the provided <see cref="ChannelOptions"/>.
        /// </summary>
        /// <param name="options">the <see cref="ChannelOptions"/> used in the new context.</param>
        /// <returns><see cref="EncodingDecodingContext"/> created with passed Channel options.</returns>
        public static EncodingDecodingContext ToEncodingDecodingContext(this ChannelOptions options)
        {
            return new EncodingDecodingContext(options ?? new ChannelOptions());
        }
    }
}
