using System.Collections.Generic;
using IO.Ably.Types;

namespace IO.Ably
{
    public enum ChannelMode
    {
        Presence,
        Publish,
        Subscribe,
        PresenceSubscribe
    }

    public static class ChannelModeExtensions
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

    public class ChannelModes : HashSet<ChannelMode>
    {
        public ChannelModes()
        {
        }

        public ChannelModes(IEnumerable<ChannelMode> modes)
            : base(modes)
        {
        }

        public void Add(params ChannelMode[] modes)
        {
            foreach (var mode in modes)
            {
                Add(mode);
            }
        }
    }
}
