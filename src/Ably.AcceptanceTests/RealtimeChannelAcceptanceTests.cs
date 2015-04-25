using NUnit.Framework;
using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Threading;

namespace Ably.AcceptanceTests
{
    [TestFixture(Protocol.MsgPack)]
    [TestFixture(Protocol.Json)]
    public class RealtimeChannelAcceptanceTests
    {
        private readonly bool _binaryProtocol;

        public RealtimeChannelAcceptanceTests(Protocol binaryProtocol)
        {
            _binaryProtocol = binaryProtocol == Protocol.MsgPack;
        }

        private AblyRealtime GetRealtimeClient(Action<AblyRealtimeOptions> setup = null)
        {
            var options = TestsSetup.GetDefaultOptions<AblyRealtimeOptions>();
            options.UseBinaryProtocol = _binaryProtocol;
            if (setup != null)
            {
                setup(options);
            }
            return new AblyRealtime(options);
        }

        [Test]
        public void TestGetChannel_ReturnsValidChannel()
        {
            // Arrange
            var client = GetRealtimeClient();
            AutoResetEvent signal = new AutoResetEvent(false);

            // Act
            Realtime.IRealtimeChannel target = client.Channels.Get("test");

            // Assert
            target.Name.ShouldBeEquivalentTo("test");
            target.State.ShouldBeEquivalentTo(Realtime.ChannelState.Initialised);
        }

        [Test]
        public void TestAttachChannel_AttachesSuccessfuly()
        {
            // Arrange
            var client = GetRealtimeClient();
            Semaphore signal = new Semaphore(0, 2);
            var args = new List<Realtime.ChannelStateChangedEventArgs>();
            Realtime.IRealtimeChannel target = client.Channels.Get("test");
            target.ChannelStateChanged += (s, e) =>
            {
                args.Add(e);
                signal.Release();
            };

            // Act
            target.Attach();

            // Assert
            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(1);
            args[0].NewState.ShouldBeEquivalentTo(Realtime.ChannelState.Attaching);
            args[0].Reason.ShouldBeEquivalentTo(null);
            target.State.ShouldBeEquivalentTo(Realtime.ChannelState.Attaching);

            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(2);
            args[1].NewState.ShouldBeEquivalentTo(Realtime.ChannelState.Attached);
            args[1].Reason.ShouldBeEquivalentTo(null);
            target.State.ShouldBeEquivalentTo(Realtime.ChannelState.Attached);
        }
    }
}
