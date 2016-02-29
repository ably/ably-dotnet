﻿using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace IO.Ably.AcceptanceTests
{
    [TestFixture( Protocol.MsgPack )]
    [TestFixture( Protocol.Json )]
    public class RealtimeAcceptanceTests
    {
        private readonly bool _binaryProtocol;

        public RealtimeAcceptanceTests( Protocol binaryProtocol )
        {
            _binaryProtocol = binaryProtocol == Protocol.MsgPack;
        }

        private AblyRealtime GetRealtimeClient( Action<AblyRealtimeOptions> setup = null )
        {
            var options = TestsSetup.GetDefaultOptions<AblyRealtimeOptions>();
            options.UseBinaryProtocol = _binaryProtocol;
            if( setup != null )
            {
                setup( options );
            }
            return new AblyRealtime( options );
        }

        [Test]
        public void TestCanConnectToAbly()
        {
            // Act
            bool result = AblyRealtime.CanConnectToAbly();

            Assert.True( result );
        }

        [Test]
        public void TestCreateRealtimeClient_IsAutoConnecting()
        {
            // Act
            var client = GetRealtimeClient();

            // Assert
            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Connecting );
            client.Connection.Close();
        }

        [Test]
        public void TestCreateRealtimeClient_ConnectsSuccessfuly()
        {
            // Act
            var client = GetRealtimeClient();
            AutoResetEvent signal = new AutoResetEvent(false);
            var args = new List<Realtime.ConnectionStateChangedEventArgs>();

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                args.Add( e );
                signal.Set();
            };

            // Assert
            signal.WaitOne( 10000 );

            args.Count.ShouldBeEquivalentTo( 1 );
            args[ 0 ].CurrentState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connected );
            args[ 0 ].PreviousState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connecting );
            args[ 0 ].Reason.ShouldBeEquivalentTo( null );
            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Connected );
        }

        [Test]
        public void TestCreateRealtimeClient_AutoConnect_False_ConnectsSuccessfuly()
        {
            // Arrange
            var client = GetRealtimeClient(o => o.AutoConnect = false);
            AutoResetEvent signal = new AutoResetEvent(false);
            var args = new List<Realtime.ConnectionStateChangedEventArgs>();

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                args.Add( e );
                if( args.Count == 2 ) signal.Set();
            };

            // Act
            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Initialized );
            client.Connect();

            // Assert
            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Connecting );

            args.Count.ShouldBeEquivalentTo( 1 );
            args[ 0 ].PreviousState.ShouldBeEquivalentTo( Realtime.ConnectionState.Initialized );
            args[ 0 ].CurrentState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connecting );
            args[ 0 ].Reason.ShouldBeEquivalentTo( null );

            signal.WaitOne( 10000 );

            args.Count.ShouldBeEquivalentTo( 2 );
            args[ 1 ].PreviousState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connecting );
            args[ 1 ].CurrentState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connected );
            args[ 1 ].Reason.ShouldBeEquivalentTo( null );

            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Connected );
        }

        [Test]
        public void TestCreateRealtimeClient_DisconnectsSuccessfuly()
        {
            // Arrange
            var client = GetRealtimeClient();
            Semaphore signal = new Semaphore(0, 3);
            var args = new List<Realtime.ConnectionStateChangedEventArgs>();

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                args.Add( e );
                signal.Release();
            };

            signal.WaitOne( 10000 );
            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Connected );

            // Act
            client.Close();

            // Assert
            signal.WaitOne( 10000 );
            args.Count.ShouldBeEquivalentTo( 2 );
            args[ 1 ].PreviousState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connected );
            args[ 1 ].CurrentState.ShouldBeEquivalentTo( Realtime.ConnectionState.Closing );
            args[ 1 ].Reason.ShouldBeEquivalentTo( ErrorInfo.ReasonClosed );

            signal.WaitOne( 10000 );
            args.Count.ShouldBeEquivalentTo( 3 );
            args[ 2 ].PreviousState.ShouldBeEquivalentTo( Realtime.ConnectionState.Closing );
            args[ 2 ].CurrentState.ShouldBeEquivalentTo( Realtime.ConnectionState.Closed );
            args[ 2 ].Reason.ShouldBeEquivalentTo( ErrorInfo.ReasonClosed );
        }

        [Test]
        public void TestRealtimeClient_Time()
        {
            // Arrange
            var client = GetRealtimeClient();
            AutoResetEvent signal = new AutoResetEvent(false);

            Tuple<DateTimeOffset?, AblyException> result = null;
            Action<DateTimeOffset?, AblyException> callback = (time, err) =>
            {
                result = Tuple.Create(time, err);
                signal.Set();
            };

            // Act
            client.Time( callback );

            // Assert
            signal.WaitOne( 10000 );
            Assert.NotNull( result );
            Assert.NotNull( result.Item1 );
            Logger.Info( "Local {0}, server {1}", DateTime.UtcNow, result.Item1.Value.DateTime );
            Assert.IsTrue( ( DateTime.UtcNow - result.Item1.Value.DateTime ).TotalSeconds < 3 );
            Assert.Null( result.Item2 );
        }

        [Test]
        public void TestRealtimeClient_Time_WhenError()
        {
            // Arrange
            var client = new AblyRealtime(new AblyRealtimeOptions("123.456:789") { Host = "nohost.tt" });
            AutoResetEvent signal = new AutoResetEvent(false);
            Tuple<DateTimeOffset?, AblyException> result = null;
            Action<DateTimeOffset?, AblyException> callback = (time, err) =>
            {
                result = Tuple.Create(time, err);
                signal.Set();
            };

            // Act
            client.Time( callback );

            // Assert
            signal.WaitOne( 10000 );
            Assert.NotNull( result );
            Assert.Null( result.Item1 );
            Assert.NotNull( result.Item2 );
        }

        [Test]
        public void TestRealtimeConnectionID_IsNullUntilConnected()
        {
            // Arrange
            var client = GetRealtimeClient(o => o.AutoConnect = false);
            AutoResetEvent signal = new AutoResetEvent(false);

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                if( e.CurrentState == Realtime.ConnectionState.Connected )
                    signal.Set();
            };
            string keyBeforeConnect = client.Connection.Id;

            // Act
            client.Connect();
            signal.WaitOne( 10000 );

            // Assert
            Assert.IsNull( keyBeforeConnect );
            Assert.IsNotNull( client.Connection.Id );
        }

        [Test]
        public void TestRealtimeConnectionID_MultipleClientsHaveDifferentIds()
        {
            const int clientCount = 5;

            // Arrange
            HashSet<string> ids = new HashSet<string>();
            Semaphore signal = new Semaphore(0, clientCount);

            // Act
            for( int i = 0; i < clientCount; i++ )
            {
                var client = GetRealtimeClient(o => o.AutoConnect = false);

                client.Connection.ConnectionStateChanged += ( s, e ) =>
                {
                    if( e.CurrentState == Realtime.ConnectionState.Connected )
                    {
                        lock ( ids )
                        {
                            ids.Add( client.Connection.Id );
                        }
                        signal.Release();
                    }
                };

                client.Connect();
            }

            // wait for all to complete
            for( int i = 0; i < clientCount; i++ )
            {
                signal.WaitOne( 10000 );
            }

            // Assert
            ids.Count.ShouldBeEquivalentTo( clientCount );
        }

        [Test]
        public void TestRealtimeConnectionKey_IsNullUntilConnected()
        {
            // Arrange
            var client = GetRealtimeClient(o => o.AutoConnect = false);
            AutoResetEvent signal = new AutoResetEvent(false);

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                if( e.CurrentState == Realtime.ConnectionState.Connected )
                    signal.Set();
            };
            string keyBeforeConnect = client.Connection.Key;

            // Act
            client.Connect();
            signal.WaitOne( 10000 );

            // Assert
            Assert.IsNull( keyBeforeConnect );
            Assert.IsNotNull( client.Connection.Key );
        }

        [Test]
        public void TestRealtimeConnectionKey_MultipleClientsHaveDifferentKeys()
        {
            const int clientCount = 5;

            // Arrange
            HashSet<string> keys = new HashSet<string>();
            Semaphore signal = new Semaphore(0, clientCount);

            // Act
            for( int i = 0; i < clientCount; i++ )
            {
                var client = GetRealtimeClient(o => o.AutoConnect = false);

                client.Connection.ConnectionStateChanged += ( s, e ) =>
                {
                    if( e.CurrentState == Realtime.ConnectionState.Connected )
                    {
                        lock ( keys )
                        {
                            keys.Add( client.Connection.Key );
                        }
                        signal.Release();
                    }
                };

                client.Connect();
            }

            // wait for all to complete
            for( int i = 0; i < clientCount; i++ )
            {
                signal.WaitOne( 10000 );
            }

            // Assert
            keys.Count.ShouldBeEquivalentTo( clientCount );
        }

        [Test]
        public void TestRealtimeClient_InvalidApiKey_GoesFailed()
        {
            // Act
            var client = GetRealtimeClient(o => o.Key = "123.456:789");
            AutoResetEvent signal = new AutoResetEvent(false);
            var args = new List<Realtime.ConnectionStateChangedEventArgs>();

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                args.Add( e );
                signal.Set();
            };

            // Assert
            signal.WaitOne( 10000 );

            args.Count.ShouldBeEquivalentTo( 1 );
            args[ 0 ].CurrentState.ShouldBeEquivalentTo( Realtime.ConnectionState.Failed );
            args[ 0 ].PreviousState.ShouldBeEquivalentTo( Realtime.ConnectionState.Connecting );
            args[ 0 ].Reason.ShouldBeEquivalentTo( client.Connection.Reason );
            client.Connection.State.ShouldBeEquivalentTo( Realtime.ConnectionState.Failed );
            client.Connection.Reason.code.ShouldBeEquivalentTo( 40400 );
            client.Connection.Reason.statusCode.ShouldBeEquivalentTo( System.Net.HttpStatusCode.NotFound );
        }

        [Test]
        public void TestRealtimeClient_ConnectionSerialIsMinus1WhenConnected()
        {
            // Act
            var client = GetRealtimeClient();
            AutoResetEvent signal = new AutoResetEvent(false);

            client.Connection.ConnectionStateChanged += ( s, e ) =>
            {
                signal.Set();
            };

            // Assert
            signal.WaitOne( 10000 );

            client.Connection.Serial.ShouldBeEquivalentTo( -1 );
        }
    }
}
