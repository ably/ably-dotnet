using System;
using System.Collections.Generic;
using System.Linq;
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
        [Trait("spec", "TR4")]
        [Trait("spec", "AD1")]
        public void ShouldHaveCorrectProperties_FlagsShouldContainBitFlags()
        {
            // TR4i
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

            // TR4a,TR4b,TR4c,TR4d,TR4e (show it is removed),TR4f,TR4g,TR4h,TR4i,TR4j,TR4k,TR4l,TR4m
            var propertyNamesAndTypes = new (string, Type)[]
            {
                ("Action", typeof(ProtocolMessage.MessageAction)),
                ("Id", typeof(string)),
                ("Auth", typeof(AuthDetails)),
                ("Channel", typeof(string)),
                ("ChannelSerial", typeof(string)),
                ("ConnectionId", typeof(string)),
                ("ConnectionSerial", typeof(long?)),
                ("ConnectionDetails", typeof(ConnectionDetails)),
                ("Count", typeof(int?)),
                ("Error", typeof(ErrorInfo)),
                ("Flags", typeof(int?)),
                ("Params", typeof(ChannelParams)),
                ("MsgSerial", typeof(long)),
                ("Messages", typeof(Message[])),
                ("Presence", typeof(PresenceMessage[])),
                ("Timestamp", typeof(DateTimeOffset?)),
            };

            var props = pm.GetType().GetProperties();
            props.Length.Should().Be(16);
            propertyNamesAndTypes.Length.Should().Be(16);

            foreach (var propertyInfo in props)
            {
                var nameAndType = (from p in propertyNamesAndTypes where p.Item1 == propertyInfo.Name select p).First();
                nameAndType.Should().NotBeNull($"Property name '{propertyInfo.Name}' not found.");
                (propertyInfo.PropertyType == nameAndType.Item2).Should().BeTrue($"The type should match but '{propertyInfo.PropertyType}' != {nameAndType.Item2}");
                propertyInfo.CanRead.Should().BeTrue();
                propertyInfo.CanWrite.Should().BeTrue();
                propertyInfo.GetGetMethod(false).IsPublic.Should().BeTrue();
                propertyInfo.GetSetMethod(false).IsPublic.Should().BeTrue();
            }

            // AD1
            var authDetails = new AuthDetails();
            var adProps = authDetails.GetType().GetProperties();
            adProps.Length.Should().Be(1);
            adProps[0].Name.Should().Be("AccessToken");
            adProps[0].CanRead.Should().BeTrue();
            adProps[0].CanWrite.Should().BeTrue();
            adProps[0].GetGetMethod(false).IsPublic.Should().BeTrue();
            adProps[0].GetSetMethod(false).IsPublic.Should().BeTrue();
        }
    }
}
