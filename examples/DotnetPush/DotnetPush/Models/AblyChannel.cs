namespace DotnetPush.ViewModels
{
    /// <summary>
    /// Describes a channel in the UI.
    /// </summary>
    public class AblyChannel
    {
        /// <summary>
        /// Name of the ably channel.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the channel.</param>
        public AblyChannel(string name)
        {
            Name = name;
        }
    }
}
