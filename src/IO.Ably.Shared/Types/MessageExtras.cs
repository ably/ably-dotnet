namespace IO.Ably.Types
{
    /// <summary>
    /// Extra properties on the Message.
    /// </summary>
    public class MessageExtras
    {
        /// <summary>
        /// Delta extras is part of the Ably delta's functionality.
        /// </summary>
        public DeltaExtras Delta { get; set; }
    }

    /// <summary>
    /// Extra message properties relating to the delta's functionality.
    /// </summary>
    public class DeltaExtras
    {
        /// <summary>
        /// Format.
        /// </summary>
        public string Fromat { get; set; }

        /// <summary>
        /// From.
        /// </summary>
        public string From { get; set; }
    }
}
