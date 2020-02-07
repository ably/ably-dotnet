using System.Collections.Generic;
using System.Collections.ObjectModel;
using IO.Ably.Types;

namespace IO.Ably
{
    /// <summary>
    /// Realtime channel modes.
    /// </summary>
    public enum ChannelMode
    {
        /// <summary>
        /// Presence mode. Allows the attached channel to enter Presence.
        /// </summary>
        Presence,

        /// <summary>
        /// Publish mode. Allows the messages to be published to the attached channel.
        /// </summary>
        Publish,

        /// <summary>
        /// Subscribe mode. Allows the attached channel to subscribe to messages.
        /// </summary>
        Subscribe,

        /// <summary>
        /// PresenceSubscribe. Allows the attached channel to subscribe to Presence updates.
        /// </summary>
        PresenceSubscribe,
    }

    /// <summary>
    /// Helper methods when dealing with Channel Models.
    /// </summary>
    internal static class ChannelModeExtensions
    {
        public static ProtocolMessage.Flag? ToFlag(this ChannelMode mode)
        {
            switch (mode)
            {
                case ChannelMode.Presence:
                    return ProtocolMessage.Flag.Presence;
                case ChannelMode.Publish:
                    return ProtocolMessage.Flag.Publish;
                case ChannelMode.Subscribe:
                    return ProtocolMessage.Flag.Subscribe;
                case ChannelMode.PresenceSubscribe:
                    return ProtocolMessage.Flag.PresenceSubscribe;
                default:
                    return null;
            }
        }

        public static IEnumerable<ChannelMode> FromFlag(this ProtocolMessage.Flag flag)
        {
            if (flag.HasFlag(ProtocolMessage.Flag.Presence))
            {
                yield return ChannelMode.Presence;
            }

            if (flag.HasFlag(ProtocolMessage.Flag.Publish))
            {
                yield return ChannelMode.Publish;
            }

            if (flag.HasFlag(ProtocolMessage.Flag.Subscribe))
            {
                yield return ChannelMode.Subscribe;
            }

            if (flag.HasFlag(ProtocolMessage.Flag.PresenceSubscribe))
            {
                yield return ChannelMode.PresenceSubscribe;
            }
        }
    }

    /// <summary>
    /// Set of Channel modes. It's used inside ChannelOptions.
    /// </summary>
    public class ChannelModes : HashSet<ChannelMode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelModes"/> class.
        /// </summary>
        public ChannelModes()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelModes"/> class.
        /// </summary>
        /// <param name="modes">A list of modes to be populated.</param>
        public ChannelModes(IEnumerable<ChannelMode> modes)
            : base(modes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelModes"/> class.
        /// </summary>
        /// <param name="modes">A list of modes to be populated.</param>
        public ChannelModes(params ChannelMode[] modes)
            : base(modes)
        {
        }
    }

    /// <summary>
    /// Read only version of <see cref="ChannelModes"/>.
    /// </summary>
    public class ReadOnlyChannelModes : ReadOnlyCollection<ChannelMode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyChannelModes"/> class.
        /// </summary>
        /// <param name="list">list of channelModes.</param>
        public ReadOnlyChannelModes(IList<ChannelMode> list)
            : base(list)
        {
        }
    }
}
