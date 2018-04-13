using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using IO.Ably.Types;
using Xunit;

namespace IO.Ably.Tests.Shared.Realtime
{
    public class ProtocolMessageTests
    {


        [Fact]
        [Trait("spec", "TR3")]
        public void ProtocolMessageFlagHaveCorrectValues()
        {
            // TR3a to TR3e
            ((int)ProtocolMessage.Flag.HasPresence).Should().Be(1 << 0);
            ((int)ProtocolMessage.Flag.HasBacklog).Should().Be(1 << 1);
            ((int)ProtocolMessage.Flag.Resumed).Should().Be(1 << 2);
            ((int)ProtocolMessage.Flag.HasLocalPresence).Should().Be(1 << 3);
            ((int)ProtocolMessage.Flag.Transient).Should().Be(1 << 4);

            // TR3q to TR3t
            ((int)ProtocolMessage.Flag.Presence).Should().Be(1 << 16);
            ((int)ProtocolMessage.Flag.Publish).Should().Be(1 << 17);
            ((int)ProtocolMessage.Flag.Subscribe).Should().Be(1 << 18);
            ((int)ProtocolMessage.Flag.PresenceSubscribe).Should().Be(1 << 19);
        }

        [Fact]
        [Trait("spec", "TR4i")]
        public void FlagsContainsBitFlags()
        {
            string messageStr = $"{{\"flags\":{(int)ProtocolMessage.Flag.HasPresence + (int)ProtocolMessage.Flag.Resumed + (int)ProtocolMessage.Flag.PresenceSubscribe}}}";

            var pm = JsonHelper.Deserialize<ProtocolMessage>(messageStr);

            pm.HasFlag(ProtocolMessage.Flag.HasPresence).Should().BeTrue();
            pm.HasFlag(ProtocolMessage.Flag.HasBacklog).Should().BeFalse();
            pm.HasFlag(ProtocolMessage.Flag.Resumed).Should().BeTrue();
            pm.HasFlag(ProtocolMessage.Flag.HasLocalPresence).Should().BeFalse();
            pm.HasFlag(ProtocolMessage.Flag.Transient).Should().BeFalse();
            pm.HasFlag(ProtocolMessage.Flag.Presence).Should().BeFalse();
            pm.HasFlag(ProtocolMessage.Flag.Publish).Should().BeFalse();
            pm.HasFlag(ProtocolMessage.Flag.Subscribe).Should().BeFalse();
            pm.HasFlag(ProtocolMessage.Flag.PresenceSubscribe).Should().BeTrue();
        }
    }
}
