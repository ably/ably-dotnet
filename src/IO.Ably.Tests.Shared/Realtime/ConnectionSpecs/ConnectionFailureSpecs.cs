using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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

        //TODO: Review Test logic
        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndRenewableToken_ShouldRenewTokenAutomaticallyWithoutEmittingError()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            bool renewTokenCalled = false;
            var client = await GetConnectedClient(
                opts =>
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
            client.Connection.On((args) =>
            {
                if (args.HasError)
                {
                    raisedErrors.Add(args.Reason);
                }
            });

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.WaitForState(ConnectionState.Failed);
            renewTokenCalled.Should().BeTrue();

            var currentToken = client.RestClient.AblyAuth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
            raisedErrors.Should().BeEmpty("No errors should be raised!");
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        [Trait("spec", "RSA4a")]
        public async Task WithTokenErrorAndNonRenewableToken_ShouldRaiseErrorAndTransitionToFailed()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            bool renewTokenCalled = false;
            var client = await GetConnectedClient(
                opts =>
            {
                opts.Key = string.Empty;
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

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.WaitForState(ConnectionState.Failed);

            renewTokenCalled.Should().BeFalse();
            client.Connection.ErrorReason.Should().NotBeNull();
            client.Connection.ErrorReason.Code.Should().Be(_tokenErrorCode);
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndTokenRenewalFails_ShouldRaiseErrorAndTransitionToDisconnected()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            var client = GetClientWithFakeTransport(
                opts =>
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

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.WaitForState(ConnectionState.Disconnected);

            client.Connection.ErrorReason.Should().NotBeNull();
            client.Connection.ErrorReason.Code.Should().Be(123);
        }

        // TODO: Revisit this test.
        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorTwice_ShouldNotRenewAndRaiseErrorAndTransitionToDisconnected()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            var renewCount = 0;
            var client = await GetConnectedClient(
                opts =>
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

            bool disconnected = false;
            client.Connection.On(ConnectionEvent.Disconnected, (_) =>
            {
                disconnected = true;
            });

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });
            await client.WaitForState(ConnectionState.Failed);

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.WaitForState(ConnectionState.Disconnected);
            renewCount.Should().Be(1);
            disconnected.Should().BeTrue();
            client.Connection.ErrorReason.Should().NotBeNull();
        }

        [Fact]
        [Trait("spec", "RTN14d")]
        [Trait("spec", "TR2")]
        public async Task WhenTransportFails_ShouldTransitionToDisconnectedAndEmitErrorWithRetry()
        {
            FakeTransportFactory.InitialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false; // this will keep it in connecting state

            ClientOptions options = null;
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                options = opts;
            });

            client.Connect();

            await client.WaitForState(ConnectionState.Connecting);

            ConnectionStateChange connectionArgs = null;
            client.Connection.On((args) =>
            {
                connectionArgs = args;
                Done();
            });

            await Task.Delay(1000); // Let the connecting state complete it's logic otherwise by the time we get to here
                                    // The transport is not created yet as this is done on a separate thread

            LastCreatedTransport.Listener.OnTransportEvent(TransportState.Closing, new Exception());

            WaitOne();
            connectionArgs.Current.Should().Be(ConnectionState.Disconnected);
            connectionArgs.Previous.Should().Be(ConnectionState.Connecting);
            connectionArgs.Event.Should().Be(ConnectionEvent.Disconnected);
            connectionArgs.RetryIn.Should().Be(options.DisconnectedRetryTimeout);
            connectionArgs.Reason.Should().NotBeNull();
        }

        [Fact(Skip = "Requires a SandBox Spec")]
        [Trait("spec", "RTN14d")]
        [Trait("spec", "RTN14e")]
        [Trait("sandboxneeded", "true")]
        public async Task WhenTransportFails_ShouldGoFromConnectingToDisconectedUntilConnectionStateTtlIsReachedAndStateIsSuspended()
        {
            Func<DateTimeOffset> nowFunc = () => DateTimeOffset.UtcNow;

            // We want access to the modified closure so we can manipulate time within ConnectionAttemptsInfo
            // ReSharper disable once AccessToModifiedClosure
            DateTimeOffset NowWrapperFn() => nowFunc();

            FakeTransportFactory.InitialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;

            // this will keep it in connecting state
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.NowFunc = NowWrapperFn;
            });

            client.Connect();
            List<ConnectionStateChange> stateChanges = new List<ConnectionStateChange>();
            client.Connection.On((args) =>
            {
                stateChanges.Add(args);
            });

            do
            {
                LastCreatedTransport.Listener?.OnTransportEvent(TransportState.Closing, new Exception());
                await WaitForConnectingOrSuspended(client);
                var now = nowFunc();
                nowFunc = () => now.AddSeconds(30);
            }
            while (client.Connection.State != ConnectionState.Suspended);

            client.Connection.State.Should().Be(ConnectionState.Suspended);

            stateChanges.Select(x => x.Current).Distinct()
                .ShouldBeEquivalentTo(new[] { ConnectionState.Connecting, ConnectionState.Disconnected, ConnectionState.Suspended, });
            int numberOfAttemps = (int)Math.Floor(Defaults.ConnectionStateTtl.TotalSeconds / 30);
            stateChanges.Count(x => x.Current == ConnectionState.Connecting).Should().Be(numberOfAttemps);
        }

        private new DateTimeOffset NowFunc()
        {
            return DateTimeOffset.UtcNow;
        }

        [Retry(3)]
        [Trait("spec", "RTN14e")]
        public async Task WhenInSuspendedState_ShouldTryAndReconnectAfterSuspendRetryTimeoutIsReached()
        {
            Func<DateTimeOffset> nowFunc = () => DateTimeOffset.UtcNow;
            DateTimeOffset NowWrapperFunc() => nowFunc();

            FakeTransportFactory.InitialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;

            // this will keep it in connecting state
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(100);
                opts.NowFunc = NowWrapperFunc;
            });

            client.Connect();
            do
            {
                LastCreatedTransport.Listener?.OnTransportEvent(TransportState.Closing, new Exception());

                await WaitForConnectingOrSuspended(client);
                nowFunc = () => DateTimeOffset.UtcNow.AddSeconds(30);
            }
            while (client.Connection.State != ConnectionState.Suspended);

            var elapsed = await client.WaitForState(ConnectionState.Connecting);
            elapsed.Should().BeCloseTo(client.Options.SuspendedRetryTimeout, 100);
        }

        private static Task WaitForConnectingOrSuspended(AblyRealtime client)
        {
            return new ConnectionAwaiter(client.Connection, ConnectionState.Connecting, ConnectionState.Suspended).Wait();
        }

        [Fact]
        [Trait("spec", "RTN14f")]
        public async Task WhenInSuspendedStateAfterRetrying_ShouldGoBackToSuspendedState()
        {
            Func<DateTimeOffset> nowFunc = () => DateTimeOffset.UtcNow;

            // We want access to the modified closure so we can manipulate time within ConnectionAttemptsInfo
            // ReSharper disable once AccessToModifiedClosure
            DateTimeOffset NowWrapperFn() => nowFunc();

            FakeTransportFactory.InitialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;

            // this will keep it in connecting state
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = false;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                opts.NowFunc = NowWrapperFn;
            });

            client.Connect();
            do
            {
                LastCreatedTransport.Listener?.OnTransportEvent(TransportState.Closing, new Exception());
                await WaitForConnectingOrSuspended(client);
                nowFunc = () => DateTimeOffset.UtcNow.AddSeconds(30);
            }
            while (client.Connection.State != ConnectionState.Suspended);

            await new ConnectionAwaiter(client.Connection, ConnectionState.Connecting).Wait();
            await new ConnectionAwaiter(client.Connection, ConnectionState.Suspended).Wait();
        }

        public ConnectingFailureSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
