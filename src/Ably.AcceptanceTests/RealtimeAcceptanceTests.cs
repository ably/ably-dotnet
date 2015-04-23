using NUnit.Framework;
using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Threading;

namespace Ably.AcceptanceTests
{
    [TestFixture(Protocol.MsgPack)]
    [TestFixture(Protocol.Json)]
    public class RealtimeAcceptanceTests
    {
        private readonly bool _binaryProtocol;

        public RealtimeAcceptanceTests(Protocol binaryProtocol)
        {
            _binaryProtocol = binaryProtocol == Protocol.MsgPack;
        }

        private AblyRealtime GetRealtimeClient()
        {
            var options = TestsSetup.GetDefaultOptions<AblyRealtimeOptions>();
            options.UseBinaryProtocol = _binaryProtocol;
            return new AblyRealtime(options);
        }

        [Test]
        public void TestCreateRealtimeClient_IsAutoConnecting()
        {
            // Act
            var client = GetRealtimeClient();
            
            // Assert
            client.Connection.State.ShouldBeEquivalentTo(Realtime.ConnectionState.Connecting);
            client.Connection.Close();
        }

        [Test]
        public void TestCreateRealtimeClient_ConnectsSuccessfuly()
        {
            // Act
            var client = GetRealtimeClient();
            AutoResetEvent signal = new AutoResetEvent(false);
            client.Connection.ConnectionStateChanged += (s, e) =>
            {
                e.CurrentState.ShouldBeEquivalentTo(Realtime.ConnectionState.Connected);
                e.PreviousState.ShouldBeEquivalentTo(Realtime.ConnectionState.Connecting);
                e.Reason.ShouldBeEquivalentTo(null);
                signal.Set();
            };

            // Assert
            signal.WaitOne(1000);
            client.Connection.State.ShouldBeEquivalentTo(Realtime.ConnectionState.Connected);
        }
    }
}
