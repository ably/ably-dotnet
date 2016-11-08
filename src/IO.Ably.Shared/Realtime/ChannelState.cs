namespace IO.Ably.Realtime
{
    /// <summary>
    /// States defined for a channel.
    /// </summary>
    public enum ChannelState
    {
        /// <summary>
        /// A channel object having this state has been initialized but no attach has 
        /// yet been attempted.
        /// </summary>
        Initialized,

        /// <summary>
        /// An attach has been initiated by sending a request to the service. This is a 
        /// transient state; it will be followed either by a transition to Attached or Failed.
        /// </summary>
        Attaching,

        /// <summary>
        /// Attach has succeeded. In the attached state a client may publish, and 
        /// subscribe to messages.
        /// </summary>
        Attached,

        /// <summary>
        /// An detach has been initiated by sending a request to the service. This is a 
        /// transient state; it will be followed either by a transition to Detached or Failed.
        /// </summary>
        Detaching,

        /// <summary>
        /// The channel, having previously been attached, has been detached.
        /// </summary>
        Detached,

        /// <summary>
        /// An indefinite failure condition. This state is entered if a channel error has 
        /// been received from the Ably service (such as an attempt to attach without the 
        /// necessary access rights).
        /// </summary>
        Failed
    }
}
