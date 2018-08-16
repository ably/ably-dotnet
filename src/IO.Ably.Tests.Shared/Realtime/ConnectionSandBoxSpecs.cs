using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("SandBox Connection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandBoxSpecs : SandboxSpecs
    {
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN6")]
        public async Task WithAutoConnectTrue_ShouldConnectToAblyInTheBackground(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await WaitForState(client);

            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN4b")]
        [Trait("spec", "RTN4d")]
        [Trait("spec", "RTN4e")]
        [Trait("spec", "RTN11a")]
        public async Task ANewConnectionShouldRaiseConnectingAndConnectedEvents(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false);
            var states = new List<ConnectionState>();
            client.Connection.On((args) =>
            {
                args.Should().BeOfType<ConnectionStateChange>();
                states.Add(args.Current);
            });

            client.Connect();

            await WaitForState(client);
            await Task.Delay(100);

            states.Should().BeEquivalentTo(new[] { ConnectionState.Connecting, ConnectionState.Connected });
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        public async Task WhenClosingAConnection_ItShouldRaiseClosingAndClosedEvents(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            // Start collecting events after the connection is open
            await WaitForState(client);

            var states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChange>();
                states.Add(args.Current);
            };

            client.Close();

            await WaitForState(client, ConnectionState.Closed, TimeSpan.FromSeconds(5));

            states.Should().BeEquivalentTo(new[] { ConnectionState.Closing, ConnectionState.Closed });
            client.Connection.State.Should().Be(ConnectionState.Closed);
        }

        [Theory]
        [ProtocolData]
        public async Task ShouldSaveConnectionStateTtlToConnectionObject(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            await WaitForState(client);

            client.Connection.ConnectionStateTtl.Should().NotBe(Defaults.ConnectionStateTtl);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN8b")]
        public async Task WithMultipleClients_ShouldHaveUniqueConnectionIdsProvidedByAbly(Protocol protocol)
        {
            var clients = new[]
            {
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol)
            };

            await Task.Delay(5000);

            var distinctConnectionIds = clients.Select(x => x.Connection.Id).Distinct();
            distinctConnectionIds.Should().HaveCount(3);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN9b")]
        public async Task WithMultipleClients_ShouldHaveUniqueConnectionKeysProvidedByAbly(Protocol protocol)
        {
            var clients = new[]
            {
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol)
            };

            // Wait for the clients to connect
            await Task.Delay(TimeSpan.FromSeconds(6));

            var distinctConnectionIds = clients.Select(x => x.Connection.Key).Distinct();
            distinctConnectionIds.Should().HaveCount(3);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN13a")]
        public async Task WithConnectedClient_PingShouldReturnServiceTime(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            await WaitForState(client);

            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14a")]
        public async Task WithInvalidApiKey_ShouldSetToFailedStateAndAddErrorMessageToEmittedState(Protocol protocol)
        {
            var invalidKey = "invalid-key".AddRandomSuffix();
            ApiKey.IsValidFormat(invalidKey).Should().BeFalse();

            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;

                // a string not in the valid key format aaaa.bbbb:cccc
                opts.Key = invalidKey;
            });

            ErrorInfo error = null;
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                error = args.Reason;
            };

            client.Connect();

            await WaitForState(client, ConnectionState.Failed);

            error.Should().NotBeNull();
            client.Connection.ErrorReason.Should().BeSameAs(error);

            // this assertion shows that we are picking up a client side validation error
            // if this key is passed to the server we would get an error with a 40005 code
            client.Connection.ErrorReason.Code.Should().Be(40101);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14b")]
        public async Task WithExpiredRenewableToken_ShouldAutomaticallyRenewTokenAndNoErrorShouldBeEmitted(Protocol protocol)
        {
            var restClient = await GetRestClient(protocol);
            var invalidToken = await restClient.Auth.RequestTokenAsync();

            // Use an old token which will result in 40143 Unrecognised token
            invalidToken.Token = invalidToken.Token.Split('.')[0] +
                                 ".DOcRVPgv1Wf1-YGgJFjyk2PNOGl_DFL7aCDzEPju8TYHorfxHHVoNoDGz5fKRW0UxePiVjD1EVEW0ZiknIK8u3S5p1FBq5Rtw_I7OX7fW8U4sGxJjAfMS_fTcXFdvouTQ";

            var realtimeClient = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = invalidToken;
                options.AutoConnect = false;
            });

            ErrorInfo error = null;
            realtimeClient.Connection.On(ConnectionEvent.Connected, (args) =>
            {
                error = args.Reason;
                ResetEvent.Set();
            });

            realtimeClient.Connect();

            ResetEvent.WaitOne(10000);

            realtimeClient.RestClient.AblyAuth.CurrentToken.Expires.Should()
                .BeAfter(TestHelpers.Now(), "The token should be valid and expire in the future.");
            error.Should().BeNull("No error should be raised!");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14c")]
        public async Task ShouldFailIfConnectionIsNotEstablishedWithInDefaultTimeout(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.RealtimeHost = "localhost";
                options.AutoConnect = false;
                options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(500);
            });

            client.Connect();

            await WaitForState(client, ConnectionState.Disconnected);
            client.Connection.ErrorReason.Should().NotBeNull();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15e")]
        public async Task ShouldUpdateConnectionKeyWhenConnectionIsResumed(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, _) => options.LogLevel = LogLevel.Debug);

            await WaitForState(client, ConnectionState.Connected);
            var initialConnectionKey = client.Connection.Key;
            var initialConnectionId = client.Connection.Id;
            client.ConnectionManager.Transport.Close(false);
            await WaitForState(client, ConnectionState.Disconnected);
            await WaitForState(client, ConnectionState.Connected, TimeSpan.FromSeconds(13));
            client.Connection.Id.Should().Be(initialConnectionId);
            client.Connection.Key.Should().NotBe(initialConnectionKey);
        }

        [Theory]
        [ProtocolData]
        public async Task WithAuthUrlShouldGetTokenFromUrl(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;

            var client = await GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var settings = await Fixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=text&body=" + token.Token;

            var authUrlClient = new AblyRealtime(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            await WaitForState(authUrlClient, waitSpan: TimeSpan.FromSeconds(5));
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15h1")]
        public async Task WhenDisconnectedMessageContainsTokenError_IfTokenIsNotRewable_ShouldBecomeFailedAndEmitError(Protocol protocol)
        {
            var awaiter = new TaskCompletionAwaiter(10000);
            var authClient = await GetRestClient(protocol);
            var tokenDetails = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromSeconds(2) });

            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TokenDetails = tokenDetails;
                options.AutoConnect = false;
            });

            client.Connect();
            await client.WaitForState(ConnectionState.Connected);

            // null the key so the token is not renewable
            client.Options.Key = null;

            client.Connection.Once(ConnectionEvent.Failed, state =>
            {
                awaiter.Tick();
            });

            client.Connection.Once(ConnectionEvent.Disconnected, state => throw new Exception("should not become DISCONNECTED"));
            client.Connection.Once(ConnectionEvent.Connected, state => throw new Exception("should not become CONNECTED"));

            await awaiter.Task;

            client.Connection.State.Should().Be(ConnectionState.Failed);
            client.Connection.ErrorReason.Should().NotBeNull();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15h2")]
        public async Task WhenDisconnectedMessageContainsTokenError_IfTokenIsRenewable_ShouldNotEmitError(Protocol protocol)
        {
            var awaiter = new TaskCompletionAwaiter(10000);
            var authClient = await GetRestClient(protocol);
            var tokenDetails = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromSeconds(2) });

            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TokenDetails = tokenDetails;
                options.DisconnectedRetryTimeout = TimeSpan.FromSeconds(1);
            });

            await client.WaitForState(ConnectionState.Connected);

            var stateChanges = new List<ConnectionStateChange>();
            client.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                stateChanges.Add(state);
                client.Connection.Once(ConnectionEvent.Connecting, state2 =>
                {
                    stateChanges.Add(state2);
                    client.Connection.Once(ConnectionEvent.Connected, state3 =>
                    {
                        client.Connection.State.Should().Be(ConnectionState.Connected);
                        client.Connection.ErrorReason.Should().BeNull();
                        stateChanges.Add(state3);
                        awaiter.SetCompleted();
                    });
                });
            });

            await awaiter.Task;
            stateChanges.Should().HaveCount(3);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15h2")]
        public async Task WhenDisconnectedMessageContainsTokenError_IfTokenRenewFails_ShouldBecomeDisconnectedAndEmitError(Protocol protocol)
        {
            var awaiter = new TaskCompletionAwaiter(10000);
            var authClient = await GetRestClient(protocol);

            var tokenDetails = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromSeconds(2) });

            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TokenDetails = tokenDetails;

                // has means to renew that should fail
                options.AuthCallback = tokenParams => throw new Exception("fail auth callback");
            });

            await client.WaitForState(ConnectionState.Connected);

            var stateChanges = new List<ConnectionState>();
            client.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                stateChanges.Add(state.Current);
                client.Connection.Once(ConnectionEvent.Connecting, state2 =>
                {
                    stateChanges.Add(state2.Current);
                    client.Connection.Once(ConnectionEvent.Disconnected, state3 =>
                    {
                        client.Connection.State.Should().Be(ConnectionState.Disconnected);
                        client.Connection.ErrorReason.Should().NotBeNull();
                        stateChanges.Add(state3.Current);
                        awaiter.SetCompleted();
                    });
                });
            });

            client.Connection.Once(ConnectionEvent.Failed, state => throw new Exception("should not become FAILED"));

            await awaiter.Task;
            stateChanges.Should().BeEquivalentTo(new[]
                { ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.Disconnected });
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15h2")]
        public async Task WhenDisconnectedMessageContainsTokenError_IfTokenRenewFailsWithFatalError_ShouldBecomeFailedAndEmitError(Protocol protocol)
        {
            var awaiter = new TaskCompletionAwaiter(10000, 3);
            var authClient = await GetRestClient(protocol);

            var tokenDetails = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromSeconds(2) });

            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TokenDetails = tokenDetails;

                // has means to renew that should result in a fatal error
                // return a 403 to simulate a fatal error, per RSA4d.
                options.AuthUrl = new Uri("https://echo.ably.io/respondwith?status=403");
            });

            await client.WaitForState(ConnectionState.Connected);

            var stateChanges = new List<ConnectionState>();
            client.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                stateChanges.Add(state.Current);
                client.Connection.Once(ConnectionEvent.Connecting, state2 =>
                {
                    stateChanges.Add(state2.Current);
                    client.Connection.Once(ConnectionEvent.Failed, state3 =>
                    {
                        client.Connection.State.Should().Be(ConnectionState.Failed);
                        client.Connection.ErrorReason.Should().NotBeNull();
                        stateChanges.Add(state3.Current);
                        awaiter.SetCompleted();
                    });
                });
            });

            await awaiter.Task;
            stateChanges.Should().BeEquivalentTo(new[]
                { ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.Failed });
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15i")]
        public async Task WithConnectedClient_WhenErrorProtocolMessageReceived_ShouldBecomeFailed(Protocol protocol)
        {
            /*
            (RTN15i)
             If an ERROR ProtocolMessage is received, this indicates a fatal error in the connection.
             The server will close the transport immediately after.
             The client should transition to the FAILED state triggering all attached channels to transition to the FAILED state as well.
             Additionally the Connection#errorReason should be set with the error received from Ably
             */

            var client = await GetRealtimeClient(protocol, (options, settings) => { });
            var channel = client.Channels.Get("RTN15i".AddRandomSuffix());
            channel.Attach();
            await channel.WaitForState(ChannelState.Attached);

            var states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();

            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
            };

            var dummyError = new ErrorInfo
            {
                Code = 40130,
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "fake error"
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = dummyError
            });

            states.Should().Equal(new[]
            {
                ConnectionState.Failed
            });

            errors.Should().HaveCount(1);
            errors[0].Should().Be(dummyError);
            channel.State.Should().Be(ChannelState.Failed);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN16d")]
        public async Task WhenRecoveringConnection_ShouldHaveSameConnectionIdButDifferentKey(Protocol protocol)
        {

        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN16d")]
        public async Task WhenConnectionFailsToRecover_ShouldEmmitDetachedMessageToChannels(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var stateChanges = new List<ChannelStateChange>();

            var client = await GetRealtimeClient(protocol);
            client.Connect();

            await WaitForState(client, ConnectionState.Connected);

            var channel1 = client.Channels.Get("test");
            channel1.On(x => stateChanges.Add(x));

            await channel1.PublishAsync("test", "best");
            await channel1.PublishAsync("test", "best");

            await Task.Delay(2000);

            client.Connection.Key = "e02789NdQA86c7!inI5Ydc-ytp7UOm3-3632e02789NdQA86c7";

            // Kill the transport
            client.ConnectionManager.Transport.Close(false);
            await Task.Delay(1000);

            await WaitForState(client, ConnectionState.Connected);
            await Task.Delay(100);

            stateChanges.Should().Contain(x => x.Current == ChannelState.Detached);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN16e")]
        public async Task WithDummyRecoverData_ShouldConnectAndSetAReasonOnTheConnection(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.Recover = "c17a8!WeXvJum2pbuVYZtF-1b63c17a8:-1";
                opts.AutoConnect = false;
            });

            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.Current == ConnectionState.Connected)
                {
                    ResetEvent.Set();
                }
            };
            client.Connect();

            var result = ResetEvent.WaitOne(10000);
            result.Should().BeTrue("Timeout");
            client.Connection.ErrorReason.Code.Should().Be(80008);
        }

        [Theory]
        [ProtocolData]
        [Trait("issue", "65")]
        public async Task WithShortlivedToken_ShouldRenewTokenMoreThanOnce(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.LogLevel = LogLevel.Debug;
            });

            var stateChanges = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();

            client.Connection.InternalStateChanged += (sender, args) =>
            {
                stateChanges.Add(args.Current);
                errors.Add(args.Reason);
            };

            await client.Auth.AuthorizeAsync(new TokenParams() { Ttl = TimeSpan.FromSeconds(5) });
            var channel = client.Channels.Get("shortToken_test" + protocol);
            int count = 0;
            while (true)
            {
                Interlocked.Increment(ref count);
                channel.Publish("test", "test");
                await Task.Delay(2000);
                if (count == 10)
                {
                    break;
                }
            }

            stateChanges.Count(x => x == ConnectionState.Connected).Should().BeGreaterThan(2);
            await client.WaitForState();
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("issue", "177")]
        public async Task WhenWebSocketClientIsNull_SendShouldSetDisconnectedState(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await WaitForState(client); // wait for connection
            client.Connection.State.Should().Be(ConnectionState.Connected);

            var transportWrapper = client.ConnectionManager.Transport as TestTransportWrapper;
            transportWrapper.Should().NotBe(null);
            var wsTransport = transportWrapper.WrappedTransport as MsWebSocketTransport;
            wsTransport.Should().NotBe(null);
            wsTransport._socket.ClientWebSocket = null;

            var tca = new TaskCompletionAwaiter();
            client.Connection.On(s =>
            {
                if (s.Current == ConnectionState.Disconnected)
                {
                    tca.SetCompleted();
                }
            });

            await client.Channels.Get("test").PublishAsync("event", "data");
            await tca.Task;

            // should auto connect again
            await WaitForState(client);
        }

        public ConnectionSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }

    [Collection("SandBox Connection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandboxTransportSideEffectsSpecs : SandboxSpecs
    {
        /*
         * (RTN19b) If there are any pending channels i.e. in the ATTACHING or DETACHING state,
         * the respective ATTACH or DETACH message should be resent to Ably
         */
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task
            WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed2(
                Protocol protocol)
        {
            var testLogger = new TestLogger("RealtimeChannel.SendMessage:Attach");
            Logger = testLogger;
            var client = await GetRealtimeClient(protocol);
            var channel = new RealtimeChannel("RTN19b", "RTN19b", client);
            channel.Logger = testLogger;
            channel.State = ChannelState.Attaching;
            channel.InternalOnInternalStateChanged(this, new ConnectionStateChange(ConnectionEvent.Connected, ConnectionState.Connected, ConnectionState.Disconnected));
            testLogger.MessageSeen.Should().Be(true);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task
            WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed(
                Protocol protocol)
        {
            int sendCount = 0;
            int tries = 0;
            while (sendCount < 2 && tries < 3)
            {
                sendCount = await WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed_count(protocol);
                tries++;
            }

            sendCount.Should().Be(2);
        }

        public async Task<int> WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed_count(
                Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var client = await GetRealtimeClient(protocol);
            var sentMessages = new List<ProtocolMessage>();
            client.SetOnTransportCreated(wrapper =>
            {
                wrapper.MessageSent = sentMessages.Add;
            });

            await client.WaitForState(ConnectionState.Connected);

            var channel = client.Channels.Get("test-channel");
            channel.Once(ChannelState.Attaching, change => client.GetTestTransport().Close(false));
            channel.Attach();
            await channel.WaitForState(ChannelState.Attaching);

            await client.WaitForState(ConnectionState.Connected);

            await Task.Delay(3000);

            return sentMessages.Count(x => x.Channel == "test-channel" && x.Action == ProtocolMessage.MessageAction.Attach);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task
            WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed2(
                Protocol protocol)
        {
            var testLogger = new TestLogger("RealtimeChannel.SendMessage:Detach");
            Logger = testLogger;
            var client = await GetRealtimeClient(protocol);
            var channel = new RealtimeChannel("RTN19b", "RTN19b", client);
            channel.Logger = testLogger;

            channel.State = ChannelState.Detaching;
            channel.InternalOnInternalStateChanged(this, new ConnectionStateChange(ConnectionEvent.Connected, ConnectionState.Connected, ConnectionState.Disconnected));

            testLogger.MessageSeen.Should().Be(true);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task
            WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed(
                Protocol protocol)
        {
            int sendCount = 0;
            int tries = 0;
            while (sendCount < 2 && tries < 3)
            {
                sendCount = await WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed_count(protocol);
                tries++;
            }

            sendCount.Should().Be(2);
        }

        private async Task<int> WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed_count(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var client = await GetRealtimeClient(protocol);
            var sentMessages = new List<ProtocolMessage>();
            client.SetOnTransportCreated(wrapper =>
            {
                wrapper.MessageSent = sentMessages.Add;
            });

            await client.WaitForState(ConnectionState.Connected);

            var channel = client.Channels.Get("test-channel");
            channel.Once(ChannelState.Detaching, change => client.GetTestTransport().Close(false));
            channel.Attach();
            channel.Detach();
            await channel.WaitForState(ChannelState.Detaching);
            await client.WaitForState(ConnectionState.Connected);

            await Task.Delay(3000);

            var y = sentMessages.Where(x => x.Channel == "test-channel" && x.Action == ProtocolMessage.MessageAction.Detach);
            return y.Count();
        }

        public ConnectionSandboxTransportSideEffectsSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }

    [Collection("SandBox Connection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandboxOperatingSystemEventsForNetworkSpecs : SandboxSpecs
    {
        [Theory(Skip= "TODO")]
#if MSGPACK
        [InlineData(Protocol.MsgPack, ConnectionState.Connected)]
        [InlineData(Protocol.MsgPack, ConnectionState.Connecting)]
#endif
        [InlineData(Protocol.Json, ConnectionState.Connected)]
        [InlineData(Protocol.Json, ConnectionState.Connecting)]
        [Trait("spec", "RTN20a")]
        public async Task
            WhenOperatingSystemNetworkIsNotAvailable_ShouldTransitionToDisconnectedAndRetry(
                Protocol protocol,
                ConnectionState initialState)
        {
            Logger.LogLevel = LogLevel.Debug;
            var client = await GetRealtimeClient(protocol, (options, _) => options.AutoConnect = false);

            client.Connect();

            await WaitForState(client, initialState);

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.On(stateChange => states.Add(stateChange.Current));

            Connection.NotifyOperatingSystemNetworkState(NetworkState.Offline, Logger);

            await Task.Delay(TimeSpan.FromSeconds(2));

            states.Should().Contain(ConnectionState.Disconnected);
            states.Should().Contain(ConnectionState.Connecting);
        }

        [Theory(Skip = "TODO")]
        [ProtocolData]
        [Trait("spec", "RTN20b")]
        public async Task
            WhenOperatingSystemNetworkBecomesAvailableAndStateIsDisconnected_ShouldTransitionTryToConnectImmediately(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, _) => options.AutoConnect = false);

            client.Connect();

            await WaitForState(client);

            await client.ConnectionManager.SetState(
                new ConnectionDisconnectedState(client.ConnectionManager, Logger) { RetryInstantly = false });

            client.Connection.State.Should().Be(ConnectionState.Disconnected);
            Connection.NotifyOperatingSystemNetworkState(NetworkState.Online, Logger);

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.On(stateChange => states.Add(stateChange.Current));

            await WaitForState(client, ConnectionState.Connecting);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN20b")]
        public async Task
            WhenOperatingSystemNetworkBecomesAvailableAndStateIsSuspended_ShouldTransitionTryToConnectImmediately(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, _) => options.AutoConnect = false);

            client.Connect();

            await WaitForState(client);

            await client.ConnectionManager.SetState(
                new ConnectionSuspendedState(client.ConnectionManager, Logger));

            client.Connection.State.Should().Be(ConnectionState.Suspended);
            Connection.NotifyOperatingSystemNetworkState(NetworkState.Online, Logger);

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.On(stateChange => states.Add(stateChange.Current));

            await WaitForState(client, ConnectionState.Connecting);
        }

        public ConnectionSandboxOperatingSystemEventsForNetworkSpecs(
            AblySandboxFixture fixture,
            ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }
}
