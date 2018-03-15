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
        [Trait("spec", "TR4i")]
        public void FlagsContainsBitFlags()
        {
            // Arrange
            int hasPresence = 1 << 0;
            int hasResumed = 1 << 2;
            int presenceSubscribe = 1 << 19;
            
            string messageStr = $"{{\"flags\":{hasPresence + hasResumed + presenceSubscribe}}}";

            // Act
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
