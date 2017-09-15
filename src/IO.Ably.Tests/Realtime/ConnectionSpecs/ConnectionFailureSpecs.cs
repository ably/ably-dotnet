using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime.ConnectionSpecs
{
    public class ConnectingFailureSpecs : ConnectionSpecsBase
    {
        private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = TestHelpers.Now().AddDays(1), ClientId = "123" };
        private int _tokenErrorCode = 40140;

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndRenewableToken_ShouldRenewTokenAutomaticallyWithoutEmittingError()
        {
            //Now = DateTimeOffset.Now;
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            bool renewTokenCalled = false;
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    renewTokenCalled = true;
                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });

            List<ErrorInfo> raisedErrors = new List<ErrorInfo>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    raisedErrors.Add(args.Reason);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            renewTokenCalled.Should().BeTrue();
            var currentToken = client.RestClient.AblyAuth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
            raisedErrors.Should().BeEmpty("No errors should be raised!");
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndNonRenewableToken_ShouldRaiseErrorAndTransitionToFailed()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            bool renewTokenCalled = false;
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.Key = "";
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    renewTokenCalled = true;
                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            renewTokenCalled.Should().BeFalse();
            client.Connection.State.Should().Be(ConnectionState.Failed);
            client.Connection.ErrorReason.Should().NotBeNull();
            client.Connection.ErrorReason.Code.Should().Be(_tokenErrorCode);
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndTokenRenewalFails_ShouldRaiseErrorAndTransitionToFailed()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    throw new AblyException(new ErrorInfo() { Code = 123 });
                }

                return AblyResponse.EmptyResponse.ToTask();
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            client.Connection.State.Should().Be(ConnectionState.Failed);
            client.Connection.ErrorReason.Should().NotBeNull();
            client.Connection.ErrorReason.Code.Should().Be(123);
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorTwice_ShouldNotRenewAndRaiseErrorAndTransitionToFailed()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            var renewCount = 0;
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    renewCount++;
                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });
            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            renewCount.Should().Be(1);
            client.Connection.State.Should().Be(ConnectionState.Failed);
            client.Connection.ErrorReason.Should().NotBeNull();
        }

        [Fact]
        [Trait("spec", "RTN14d")]
        public async Task WhenTransportFails_ShouldTransitionToDisconnectedAndEmitErrorWithRetry()
        {
            _fakeTransportFactory.initialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false; //this will keep it in connecting state

            ClientOptions options = null;
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                options = opts;
            });

            client.Connect();

            await WaitForConnectingOrSuspended(client);
            ConnectionStateChange connectionArgs = null;
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                connectionArgs = args;
                Done();
            };
            LastCreatedTransport.Listener.OnTransportEvent(TransportState.Closing, new Exception());

            WaitOne();
            connectionArgs.Current.Should().Be(ConnectionState.Disconnected);
            connectionArgs.RetryIn.Should().Be(options.DisconnectedRetryTimeout);
            connectionArgs.Reason.Should().NotBeNull();
        }

        [Fact(Skip = "Requires a SandBox Spec")]
        [Trait("spec", "RTN14d")]
        [Trait("spec", "RTN14e")]
        [Trait("sandboxneeded", "true")]
        public async Task WhenTransportFails_ShouldGoFromConnectingToDisconectedUntilConnectionStateTtlIsReachedAndStateIsSuspended()
        {
            SetNowFunc(() => DateTimeOffset.UtcNow);

            _fakeTransportFactory.initialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;
            //this will keep it in connecting state

            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
            });

            client.Connect();
            List<ConnectionStateChange> stateChanges = new List<ConnectionStateChange>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                stateChanges.Add(args);
            };

            do
            {
                LastCreatedTransport.Listener?.OnTransportEvent(TransportState.Closing, new Exception());
                await WaitForConnectingOrSuspended(client);
                SetNowFunc(() => DateTimeOffset.UtcNow.AddSeconds(30));
            } while (client.Connection.State != ConnectionState.Suspended);

            client.Connection.State.Should().Be(ConnectionState.Suspended);

            stateChanges.Select(x => x.Current).Distinct()
                .ShouldBeEquivalentTo(new[] { ConnectionState.Connecting, ConnectionState.Disconnected, ConnectionState.Suspended, });
            int numberOfAttemps = (int)Math.Floor(Defaults.ConnectionStateTtl.TotalSeconds / 30);
            stateChanges.Count(x => x.Current == ConnectionState.Connecting).Should().Be(numberOfAttemps);
        }

        private DateTimeOffset NowFunc()
        {
            return DateTimeOffset.UtcNow;
        }

        [Fact]
        [Trait("spec", "RTN14e")]
        public async Task WhenInSuspendedState_ShouldTryAndReconnectAfterSuspendRetryTimeoutIsReached()
        {
            SetNowFunc(() => DateTimeOffset.UtcNow);

            _fakeTransportFactory.initialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;
            //this will keep it in connecting state

            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(100);
                opts.NowProvider = NowProvider;
            });

            client.Connect();
            do
            {
                LastCreatedTransport.Listener?.OnTransportEvent(TransportState.Closing, new Exception());
                
                await WaitForConnectingOrSuspended(client);
                NowAddSeconds(30);

            } while (client.Connection.State != ConnectionState.Suspended);

            var awaiter = new ConnectionAwaiter(client.Connection, ConnectionState.Connecting);
            var elapsed = await awaiter.Wait();
            elapsed.Should().BeCloseTo(client.Options.SuspendedRetryTimeout, 100);
        }

        private static async Task WaitForConnectingOrSuspended(AblyRealtime client)
        {
            await
                Task.WhenAll(
                    new ConnectionAwaiter(client.Connection, ConnectionState.Connecting, ConnectionState.Suspended).Wait
                        (),
                    Task.Delay(10));
        }

        [Fact]
        [Trait("spec", "RTN14f")]
        public async Task WhenInSuspendedStateAfterRetrying_ShouldGoBackToSuspendedState()
        {
            SetNowFunc(() => DateTimeOffset.UtcNow);

            _fakeTransportFactory.initialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;
            //this will keep it in connecting state

            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
            });

            client.Connect();
            do
            {
                LastCreatedTransport.Listener?.OnTransportEvent(TransportState.Closing, new Exception());
                await WaitForConnectingOrSuspended(client);
                NowAddSeconds(30);
            } while (client.Connection.State != ConnectionState.Suspended);

            await new ConnectionAwaiter(client.Connection, ConnectionState.Connecting).Wait();
            await new ConnectionAwaiter(client.Connection, ConnectionState.Suspended).Wait();
        }

        public ConnectingFailureSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}
