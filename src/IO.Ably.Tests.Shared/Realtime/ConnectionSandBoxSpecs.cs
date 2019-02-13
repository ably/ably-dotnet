using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        [Trait("spec", "RTN11b")]
        public async Task WithClosingConnection_WhenConnectCalled_ShouldMakeNewConnectionAndTransport(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.DisconnectedRetryTimeout = TimeSpan.MaxValue;
            });

            await client.WaitForState();

            // capture initial values
            var initialConnection = client.Connection;
            var initialConnectionId = client.Connection.Id;
            var initialTransport = client.ConnectionManager.Transport;

            // The close timeout is 1000ms, so 3000ms is enough time to wait
            var awaiter = new TaskCompletionAwaiter(3000);
            client.Connection.On(ConnectionEvent.Closed, (state) =>
            {
                awaiter.SetCompleted();
            });

            client.Close();
            await client.WaitForState(ConnectionState.Closing);

            client.Connect();
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.Id.Should().NotBeNullOrEmpty();
            client.Connection.Id.Should().NotBe(initialConnectionId);
            client.ConnectionManager.Transport.Should().NotBe(initialTransport);

            // because a new transport is created the CLOSED message for the
            // old connection never arrives.
            var didClose = await awaiter.Task;
            didClose.Should().BeFalse();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN11c")]
        public async Task WithDisconnectedConnection_WhenConnectCalled_ImmediatelyReconnect(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await client.WaitForState();
            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));
            await client.WaitForState(ConnectionState.Disconnected);
            var s = new Stopwatch();
            s.Start();
            client.Connect();
            await client.WaitForState(ConnectionState.Connecting);
            client.Connection.State.Should().Be(ConnectionState.Connecting);
            s.Stop();

            // show the reconnect happened before the retry timeout could have fired
            s.Elapsed.Should().BeLessThan(client.Options.DisconnectedRetryTimeout);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN11c")]
        public async Task WithSuspendedConnection_WhenConnectCalled_ImmediatelyReconnect(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await client.ConnectionManager.SetState(new ConnectionSuspendedState(client.ConnectionManager, new ErrorInfo("force suspended"), client.Logger));
            await client.WaitForState(ConnectionState.Suspended);
            var s = new Stopwatch();
            s.Start();
            client.Connect();
            await client.WaitForState(ConnectionState.Connecting);
            client.Connection.State.Should().Be(ConnectionState.Connecting);
            s.Stop();

            // show the reconnect happened before the retry timeout should fired
            s.Elapsed.Should().BeLessThan(client.Options.DisconnectedRetryTimeout);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN11d")]
        public async Task WithFailedConnection_WhenConnectCalled_TransitionsChannelsToInitialized(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await client.WaitForState(ConnectionState.Connected);

            var chan1 = client.Channels.Get("RTN11d".AddRandomSuffix());
            await chan1.AttachAsync();

            // show that the channel is not in the initialized state already
            chan1.State.Should().NotBe(ChannelState.Initialized);

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));
            await client.WaitForState(ConnectionState.Disconnected);
            await client.ConnectionManager.SetState(new ConnectionFailedState(client.ConnectionManager, new ErrorInfo("force failed"), client.Logger));
            await client.WaitForState(ConnectionState.Failed);

            // show there is a no-null error present on the connection
            client.Connection.ErrorReason.Message.Should().Be("force failed");
            client.Connect();
            await client.WaitForState(ConnectionState.Connecting);
            client.Connection.State.Should().Be(ConnectionState.Connecting);

            // transitions all the channels to INITIALIZED
            chan1.State.Should().Be(ChannelState.Initialized);

            // sets their errorReason to null
            chan1.ErrorReason.Should().BeNull();

            // and sets the connections errorReason to null
            client.Connection.ErrorReason.Should().BeNull();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN12d")]
        public async Task WithDisconnectedOrSuspendedConnection_WhenCloseCalled_AbortRetryAndCloseImmediately(Protocol protocol)
        {
            async Task AssertsClosesAndDoesNotReconnect(AblyRealtime realtime, ConnectionState state)
            {
                await realtime.WaitForState(state);

                var reconnectAwaiter = new TaskCompletionAwaiter(5000);
                realtime.Connection.On(args =>
                {
                    if (realtime.Connection.State == ConnectionState.Connecting
                        || realtime.Connection.State == ConnectionState.Connected)
                    {
                        reconnectAwaiter.SetCompleted();
                    }
                });

                realtime.Close();
                await realtime.WaitForState(ConnectionState.Closed);
                realtime.Connection.State.Should().Be(ConnectionState.Closed);

                var didReconnect = await reconnectAwaiter.Task;
                didReconnect.Should().BeFalse($"should not attempt a reconnect for state {state}");
            }

            // setup a new client and put into a DISCONNECTED state
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.DisconnectedRetryTimeout = TimeSpan.FromSeconds(2);
            });

            await client.WaitForState();
            await client.ConnectionManager.SetState(new ConnectionDisconnectedState(client.ConnectionManager, new ErrorInfo("force disconnect"), client.Logger));
            await AssertsClosesAndDoesNotReconnect(client, ConnectionState.Disconnected);

            // reinitialize the client and put into a SUSPENDED state
            client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.SuspendedRetryTimeout = TimeSpan.FromSeconds(2);
            });

            await client.WaitForState();
            await client.ConnectionManager.SetState(new ConnectionSuspendedState(client.ConnectionManager, new ErrorInfo("force suspend"), client.Logger));
            await AssertsClosesAndDoesNotReconnect(client, ConnectionState.Suspended);
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
        [Trait("spec", "RTN15c1")]
        public async Task ResumeRequest_ConnectedProtocolMessageWithSameConnectionId_WithNoError(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            var channel = client.Channels.Get("RTN15c1".AddRandomSuffix()) as RealtimeChannel;
            await client.WaitForState(ConnectionState.Connected);
            var connectionId = client.Connection.Id;
            channel.Attach();
            await channel.WaitForState(ChannelState.Attached);
            channel.State.Should().Be(ChannelState.Attached);

            // kill the transport so the connection becomes DISCONNECTED
            client.ConnectionManager.Transport.Close(false);
            await client.WaitForState(ConnectionState.Disconnected);

            var awaiter = new TaskCompletionAwaiter(15000);
            client.Connection.Once(ConnectionEvent.Connected, change =>
            {
                change.HasError.Should().BeFalse();
                awaiter.SetCompleted();
            });

            channel.Publish(null, "foo");

            // currently disconnected so message is queued
            client.ConnectionManager.PendingMessages.Should().HaveCount(1);

            // wait for reconnection
            var didConnect = await awaiter.Task;
            didConnect.Should().BeTrue();

            // we should have received a CONNECTED Protocol message with a corresponding connectionId
            client.GetTestTransport().ProtocolMessagesReceived.Count(x => x.Action == ProtocolMessage.MessageAction.Connected).Should().Be(1);
            var connectedProtocolMessage = client.GetTestTransport().ProtocolMessagesReceived.First(x => x.Action == ProtocolMessage.MessageAction.Connected);
            connectedProtocolMessage.ConnectionId.Should().Be(connectionId);

            // channel should be attached and pending messages sent
            channel.State.Should().Be(ChannelState.Attached);
            client.ConnectionManager.PendingMessages.Should().HaveCount(0);

            // clean up
            client.Close();
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
            stateChanges[0].HasError.Should().BeTrue();
            stateChanges[0].Reason.Code.Should().Be(40142);
            stateChanges[1].HasError.Should().BeFalse();
            stateChanges[2].HasError.Should().BeFalse();
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

            var stateChanges = new List<ConnectionStateChange>();
            client.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                stateChanges.Add(state);
                client.Connection.Once(ConnectionEvent.Connecting, state2 =>
                {
                    stateChanges.Add(state2);
                    client.Connection.Once(ConnectionEvent.Disconnected, state3 =>
                    {
                        client.Connection.State.Should().Be(ConnectionState.Disconnected);
                        client.Connection.ErrorReason.Should().NotBeNull();
                        stateChanges.Add(state3);
                        awaiter.SetCompleted();
                    });
                });
            });

            client.Connection.Once(ConnectionEvent.Failed, state => throw new Exception("should not become FAILED"));

            await awaiter.Task;
            stateChanges.Select(x => x.Current).Should().BeEquivalentTo(new[]
                { ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.Disconnected });

            stateChanges[0].HasError.Should().BeTrue();
            stateChanges[0].Reason.Code.Should().Be(40142);
            stateChanges[1].HasError.Should().BeFalse();
            stateChanges[2].HasError.Should().BeTrue();
            stateChanges[2].Reason.Code.Should().Be(80019);
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

            var stateChanges = new List<ConnectionStateChange>();
            client.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                stateChanges.Add(state);
                client.Connection.Once(ConnectionEvent.Connecting, state2 =>
                {
                    stateChanges.Add(state2);
                    client.Connection.Once(ConnectionEvent.Failed, state3 =>
                    {
                        client.Connection.State.Should().Be(ConnectionState.Failed);
                        client.Connection.ErrorReason.Should().NotBeNull();
                        stateChanges.Add(state3);
                        awaiter.SetCompleted();
                    });
                });
            });

            await awaiter.Task;
            stateChanges.Select(x => x.Current).Should().BeEquivalentTo(new[]
                { ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.Failed });

            stateChanges[0].HasError.Should().BeTrue();
            stateChanges[0].Reason.Code.Should().Be(40142);
            stateChanges[1].HasError.Should().BeFalse();
            stateChanges[2].HasError.Should().BeTrue();
            stateChanges[2].Reason.Code.Should().Be(80019);
            stateChanges[2].Reason.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15g")]
        [Trait("spec", "RTN15g1")]
        [Trait("spec", "RTN15g2")]
        [Trait("spec", "RTN15g3")]
        public async Task WhenDisconnectedPastTTL_ShouldNotResume_ShouldClearConnectionStateAndAttemptNewConnection(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(1000);
                options.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(5000);
            });

            DateTime disconnectedAt = DateTime.MinValue;
            DateTime reconnectedAt = DateTime.MinValue;
            TimeSpan connectionStateTtl = TimeSpan.MinValue;
            string initialConnectionId = string.Empty;
            string newConnectionId = string.Empty;

            await client.WaitForState(ConnectionState.Connected);

            client.Connection.ConnectionStateTtl = TimeSpan.FromSeconds(1);
            initialConnectionId = client.Connection.Id;
            connectionStateTtl = client.Connection.ConnectionStateTtl;

            var aliveAt1 = client.Connection.ConfirmedAliveAt;
            var aliveAt2 = aliveAt1;

            // RTN15g3 ATTACHED, ATTACHING, or SUSPENDED must be automatically reattached
            var channels = new List<RealtimeChannel>();
            channels.Add(client.Channels.Get("attached".AddRandomSuffix()) as RealtimeChannel);
            channels.Add(client.Channels.Get("attaching".AddRandomSuffix()) as RealtimeChannel);
            channels.Add(client.Channels.Get("suspended".AddRandomSuffix()) as RealtimeChannel);
            channels[2].State = ChannelState.Suspended;

            channels[0].Attach();
            await channels[0].WaitForState();

            channels[0].State.Should().Be(ChannelState.Attached);
            channels[1].State.Should().Be(ChannelState.Initialized); // set attaching later
            channels[2].State.Should().Be(ChannelState.Suspended);

            await WaitFor(60000, async done =>
            {
                client.Connection.Once(ConnectionEvent.Disconnected, change2 =>
                {
                    disconnectedAt = DateTime.UtcNow;
                    channels[1].Attach(); // connection disconnected so this should become attaching
                    channels[1].WaitForState(ChannelState.Attaching);
                    client.Connection.Once(ConnectionEvent.Connecting, change3 =>
                    {
                        reconnectedAt = DateTime.UtcNow;
                        client.Connection.Once(ConnectionEvent.Connected, change4 =>
                        {
                            newConnectionId = client.Connection.Id;
                            aliveAt2 = client.Connection.ConfirmedAliveAt;
                            done();
                        });
                    });
                });

                client.GetTestTransport().Close(); // close event is surpressed by default
                await client.ConnectionManager.SetState(new ConnectionDisconnectedState(client.ConnectionManager, ErrorInfo.ReasonDisconnected, client.Logger));
            });

            var interval = reconnectedAt - disconnectedAt;
            interval.TotalMilliseconds.Should().BeGreaterThan(5000);
            initialConnectionId.Should().NotBeNullOrEmpty();
            initialConnectionId.Should().NotBe(newConnectionId);
            connectionStateTtl.Should().Be(TimeSpan.FromSeconds(1));
            aliveAt1.Value.Should().BeBefore(aliveAt2.Value);

            await channels[0].WaitForState(ChannelState.Attached);
            await channels[1].WaitForState(ChannelState.Attached);
            await channels[2].WaitForState(ChannelState.Attached);

            channels[0].State.Should().Be(ChannelState.Attached);
            channels[1].State.Should().Be(ChannelState.Attached);
            channels[2].State.Should().Be(ChannelState.Attached);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15g")]
        [Trait("spec", "RTN15g1")]
        public async Task WhenDisconnectedIsNotPastTTL_ShouldResume_ShouldClearConnectionStateAndAttemptNewConnection(
            Protocol protocol)
        {
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

            channel1.Attach();
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
                opts.Recover = "c17a8!WeXvJum2pbuVYZtF-1b63c17a8:-1:-1";
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
        public async Task WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed(Protocol protocol)
        {
            var channelName = "test-channel".AddRandomSuffix();
            var sentMessages = new List<ProtocolMessage>();
            Logger.LogLevel = LogLevel.Debug;
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory()
                {
                    OnMessageSent = sentMessages.Add
                };
            });

            await client.WaitForState(ConnectionState.Connected);

            var transport = client.GetTestTransport();
            transport.MessageSent = sentMessages.Add;

            var channel = client.Channels.Get(channelName);
            channel.Once(ChannelEvent.Attaching, change =>
            {
                transport.Close(false);
            });
            channel.Attach();
            await channel.WaitForState(ChannelState.Attaching);
            bool didDisconnect = false;
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                didDisconnect = true;
                sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach).Should().Be(1);
            });

            await client.WaitForState(ConnectionState.Disconnected);
            await client.WaitForState(ConnectionState.Connecting);
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.State.Should().Be(ConnectionState.Connected);
            didDisconnect.Should().BeTrue();

            await channel.WaitForState(ChannelState.Attached);

            var attachCount = sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach);
            attachCount.Should().Be(2);

            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed(Protocol protocol)
        {
            var channelName = "test-channel".AddRandomSuffix();
            var sentMessages = new List<ProtocolMessage>();
            Logger.LogLevel = LogLevel.Debug;
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory()
                {
                    OnMessageSent = sentMessages.Add
                };
            });

            await client.WaitForState(ConnectionState.Connected);

            var transport = client.GetTestTransport();
            transport.MessageSent = sentMessages.Add;

            var channel = client.Channels.Get(channelName);
            channel.Once(ChannelEvent.Detaching, change =>
            {
                transport.Close(false);
            });
            channel.Attach();
            await channel.WaitForState(ChannelState.Attached);
            channel.Detach();
            await channel.WaitForState(ChannelState.Detaching);
            bool didDisconnect = false;
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                didDisconnect = true;
                sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach).Should().Be(1);
            });

            await client.WaitForState(ConnectionState.Disconnected);
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.State.Should().Be(ConnectionState.Connected);
            didDisconnect.Should().BeTrue();

            await channel.WaitForState(ChannelState.Detached);

            var detatchCount = sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Detach);
            detatchCount.Should().Be(2);

            client.Close();
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

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN22")]
        public async Task WhenAuthMessageReceived_ShouldAttemptTokenRenewal(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.UseTokenAuth = true;
            });

            await client.WaitForState(ConnectionState.Connected);

            var initialToken = client.RestClient.AblyAuth.CurrentToken;
            var initialClientId = client.ClientId;

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Auth));

            await Task.Delay(1000);

            client.RestClient.AblyAuth.CurrentToken.Should().NotBe(initialToken);
            client.ClientId.Should().Be(initialClientId);
            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN22a")]
        public async Task WhenFakeDisconnectedMessageContainsTokenError_ForcesClientToReauthenticate(Protocol protocol)
        {

            var reconnectAwaiter = new TaskCompletionAwaiter();
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.UseTokenAuth = true;
            });

            await client.WaitForState(ConnectionState.Connected);

            var initialToken = client.RestClient.AblyAuth.CurrentToken;

            client.Connection.Once(ConnectionEvent.Disconnected, state2 =>
            {
                client.Connection.Once(ConnectionEvent.Connected, state3 =>
                {
                    reconnectAwaiter.SetCompleted();
                });
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { Error = new ErrorInfo("testing RTN22a", 40140) });
            var didReconect = await reconnectAwaiter.Task;
            didReconect.Should().BeTrue();
            client.RestClient.AblyAuth.CurrentToken.Should().NotBe(initialToken);
            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN22a")]
        public async Task WhenDisconnectedMessageContainsTokenError_ForcesClientToReauthenticate(Protocol protocol)
        {
            var authClient = await GetRestClient(protocol);

            var reconnectAwaiter = new TaskCompletionAwaiter(60000);
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.AuthCallback = tokenParams =>
                {
                    var results = authClient.AblyAuth.RequestToken(new TokenParams { ClientId = "RTN22a", Ttl = TimeSpan.FromSeconds(35) });
                    return Task.FromResult<object>(results);
                };
                options.ClientId = "RTN22a";
            });

            await client.WaitForState(ConnectionState.Connected);

            var initialToken = client.RestClient.AblyAuth.CurrentToken;

            client.Connection.Once(ConnectionEvent.Disconnected, state2 =>
            {
                client.Connection.Once(ConnectionEvent.Connected, state3 =>
                {
                    reconnectAwaiter.SetCompleted();
                });
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { Error = new ErrorInfo("testing RTN22a", 40140) });
            var didReconect = await reconnectAwaiter.Task;
            didReconect.Should().BeTrue();
            client.RestClient.AblyAuth.CurrentToken.Should().NotBe(initialToken);
            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN24")]
        [Trait("spec", "RTN21")]
        [Trait("spec", "RTN4h")]
        [Trait("spec", "RTC8a1")]
        public async Task WhenConnectedMessageReceived_ShouldEmitUpdate(Protocol protocol)
        {
            var updateAwaiter = new TaskCompletionAwaiter(5000);
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.UseTokenAuth = true;
                options.AutoConnect = true;
            });

            await client.WaitForState(ConnectionState.Connected);

            client.Connection.ConnectionStateTtl.Should().NotBe(TimeSpan.MaxValue);

            var key = client.Connection.Key;

            client.Connection.Once(state =>
            {
                // RTN4h - can emit UPDATE event
                if (state.Event == ConnectionEvent.Update)
                {
                    // should have both previous and current attributes set to CONNECTED
                    state.Current.Should().Be(ConnectionState.Connected);
                    state.Previous.Should().Be(ConnectionState.Connected);
                    state.Reason.Message = "fake-error";
                    updateAwaiter.SetCompleted();
                }
                else
                {
                    throw new Exception($"'{state.Event}' was handled. Only an 'Update' event should have occured");
                }
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                ConnectionDetails = new ConnectionDetails
                {
                    ConnectionKey = "key",
                    ClientId = "RTN21",
                    ConnectionStateTtl = TimeSpan.MaxValue
                },
                Error = new ErrorInfo("fake-error")
            });

            var didUpdate = await updateAwaiter.Task;
            didUpdate.Should().BeTrue();

            // RTN21 - new connection details over write old values
            client.Connection.Key.Should().NotBe(key);
            client.ClientId.Should().Be("RTN21");
            client.Connection.ConnectionStateTtl.Should().Be(TimeSpan.MaxValue);
        }

        public ConnectionSandboxOperatingSystemEventsForNetworkSpecs(
            AblySandboxFixture fixture,
            ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }
}
