using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.AcceptanceTests
{
    [TestFixture(Protocol.MsgPack)]
    [TestFixture(Protocol.Json)]
    [Ignore("Will get those fixed after getting the rest tests to work")]
    public class RealtimeAcceptanceTests
    {
        private readonly bool _binaryProtocol;

        public RealtimeAcceptanceTests(Protocol binaryProtocol)
        {
            _binaryProtocol = binaryProtocol == Protocol.MsgPack;
        }

        private AblyRealtime GetRealtimeClient(Action<ClientOptions> setup = null)
        {
            var options = TestsSetup.GetDefaultOptions<ClientOptions>();
            options.UseBinaryProtocol = _binaryProtocol;
            if (setup != null)
            {
                setup(options);
            }
            return new AblyRealtime(options);
        }

        [Test]
        public void TestCreateRealtimeClient_DisconnectsSuccessfuly()
        {
            // Arrange
            var client = GetRealtimeClient();
            Semaphore signal = new Semaphore(0, 3);
            var args = new List<Realtime.ConnectionStateChangedEventArgs>();

            client.Connection.ConnectionStateChanged += (s, e) =>
            {
                args.Add(e);
                signal.Release();
            };

            signal.WaitOne(10000);
            client.Connection.State.ShouldBeEquivalentTo(Realtime.ConnectionStateType.Connected);

            // Act
            client.Close();

            // Assert
            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(2);
            args[1].PreviousState.ShouldBeEquivalentTo(Realtime.ConnectionStateType.Connected);
            args[1].CurrentState.ShouldBeEquivalentTo(Realtime.ConnectionStateType.Closing);
            args[1].Reason.ShouldBeEquivalentTo(ErrorInfo.ReasonClosed);

            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(3);
            args[2].PreviousState.ShouldBeEquivalentTo(Realtime.ConnectionStateType.Closing);
            args[2].CurrentState.ShouldBeEquivalentTo(Realtime.ConnectionStateType.Closed);
            args[2].Reason.ShouldBeEquivalentTo(ErrorInfo.ReasonClosed);
        }

        static Tuple<DateTimeOffset?, AblyException> Time(AblyRealtime client)
        {
            try
            {
                DateTimeOffset? dt = client.Time().Result;
                return new Tuple<DateTimeOffset?, AblyException>(dt, null);
            }
            catch (Exception ex)
            {
                return new Tuple<DateTimeOffset?, AblyException>(null, new AblyException(ex));
            }
        }

        [Test]
        public void TestRealtimeClient_Time()
        {
            // Arrange
            var client = GetRealtimeClient();

            var result = Time(client);

            Assert.NotNull(result);
            Assert.NotNull(result.Item1);
            Logger.Info("Local {0}, server {1}", DateTimeOffset.UtcNow, result.Item1.Value);
            Assert.IsTrue((DateTimeOffset.UtcNow - result.Item1.Value).TotalSeconds < 3);
            Assert.Null(result.Item2);
        }

        [Test]
        public void TestRealtimeClient_Time_WhenError()
        {
            // Arrange
            var client = new AblyRealtime(new ClientOptions("123.456:789") { RealtimeHost = "nohost.tt" });
            AutoResetEvent signal = new AutoResetEvent(false);

            var result = Time(client);

            // Assert
            signal.WaitOne(10000);
            Assert.NotNull(result);
            Assert.Null(result.Item1);
            Assert.NotNull(result.Item2);
        }

        [Test]
        public async Task TestRealtimeConnectionID_IsNullUntilConnected()
        {
            // Arrange
            var client = GetRealtimeClient(o => o.AutoConnect = false);
            AutoResetEvent signal = new AutoResetEvent(false);

            client.Connection.ConnectionStateChanged += (s, e) =>
            {
                if (e.CurrentState == Realtime.ConnectionStateType.Connected)
                    signal.Set();
            };
            string keyBeforeConnect = client.Connection.Id;

            // Act
            client.Connect();
            signal.WaitOne(10000);

            // Assert
            Assert.IsNull(keyBeforeConnect);
            Assert.IsNotNull(client.Connection.Id);
        }

        [Test]
        public async Task TestRealtimeConnectionKey_IsNullUntilConnected()
        {
            // Arrange
            var client = GetRealtimeClient(o => o.AutoConnect = false);
            AutoResetEvent signal = new AutoResetEvent(false);

            client.Connection.ConnectionStateChanged += (s, e) =>
            {
                if (e.CurrentState == Realtime.ConnectionStateType.Connected)
                    signal.Set();
            };
            string keyBeforeConnect = client.Connection.Key;

            // Act
            client.Connect();
            signal.WaitOne(10000);

            // Assert
            Assert.IsNull(keyBeforeConnect);
            Assert.IsNotNull(client.Connection.Key);
        }
        
        [Test]
        public void TestRealtimeClient_ConnectionSerialIsMinus1WhenConnected()
        {
            // Act
            var client = GetRealtimeClient();
            AutoResetEvent signal = new AutoResetEvent(false);

            client.Connection.ConnectionStateChanged += (s, e) =>
            {
                signal.Set();
            };

            // Assert
            signal.WaitOne(10000);

            client.Connection.Serial.ShouldBeEquivalentTo(-1);
        }
    }
}
