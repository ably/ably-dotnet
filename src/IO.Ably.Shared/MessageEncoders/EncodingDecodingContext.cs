using IO.Ably.Transport;

namespace IO.Ably.MessageEncoders
{
    /// <summary>
    /// Class used to provide context between different encoders.
    /// </summary>
    internal class EncodingDecodingContext
    {
        /// <summary>
        /// It stores the encoding of PreviousPayload. It's used for debugging purposes.
        /// </summary>
        public string PreviousPayloadEncoding { get; set; }

        /// <summary>
        /// the base payload of the previous message (ie without any transport-specific encoding step).
        /// </summary>
        public byte[] PreviousPayload { get; set; }

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
            ChannelOptions = new ChannelOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingDecodingContext"/> class.
        /// </summary>
        /// <param name="options">Channel options used for the encode / decode operations.</param>
        public EncodingDecodingContext(ChannelOptions options)
        {
            ChannelOptions = options ?? new ChannelOptions();
        }
    }

    /// <summary>
    /// Helpers methods for <see cref="EncodingDecodingContext"/>.
    /// </summary>
    internal static class EncodingDecodingContextHelpers
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
