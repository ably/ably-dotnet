using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Transport;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime.ConnectionSpecs
{
    public class ConnectingFailureSpecs : AblyRealtimeSpecs
    {
        private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = TestHelpers.Now().AddDays(1), ClientId = "123" };
        private int _tokenErrorCode = 40140;

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndRenewableToken_ShouldRenewTokenAutomaticallyWithoutEmittingError()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            bool renewTokenCalled = false;
            var client = GetClientWithFakeTransport(
                opts =>
            {
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
                opts.AutoConnect = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    if (renewTokenCalled == false)
                    {
                        renewTokenCalled = true;
                    }

                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });
            client.Connect();
            List<ErrorInfo> raisedErrors = new List<ErrorInfo>();
            client.Connection.On((args) =>
            {
                if (args.HasError)
                {
                    raisedErrors.Add(args.Reason);
                }
            });

            await client.WaitForState(ConnectionState.Connecting);

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.ProcessCommands();
            renewTokenCalled.Should().BeTrue();
            var currentToken = client.RestClient.AblyAuth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
            raisedErrors.Should().BeEmpty("No errors should be raised!");
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorAndTokenRenewalFails_ShouldRaiseErrorAndTransitionToDisconnected()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            var taskAwaiter = new TaskCompletionAwaiter(taskCount: 2);
            var client = GetClientWithFakeTransport(
                opts =>
            {
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    throw new AblyException(new ErrorInfo() { Code = _tokenErrorCode });
                }

                return AblyResponse.EmptyResponse.ToTask();
            });

            var stateChanges = new List<ConnectionStateChange>();
            client.Connection.On(stateChange =>
            {
                if (stateChange.Current == ConnectionState.Disconnected)
                {
                    taskAwaiter.Tick();
                }

                stateChanges.Add(stateChange);
            });

            await client.WaitForState(ConnectionState.Connecting);

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await taskAwaiter.Task;

            client.Connection.ErrorReason.Should().NotBeNull();
            client.Connection.ErrorReason.Code.Should().Be(_tokenErrorCode);
        }

        [Fact]
        [Trait("spec", "RTN14b")]
        public async Task WithTokenErrorTwice_ShouldNotRenewAndRaiseErrorAndTransitionToDisconnected()
        {
            var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
            var renewCount = 0;
            var client = GetClientWithFakeTransport(
                opts =>
            {
                opts.TokenDetails = tokenDetails;
                opts.UseBinaryProtocol = false;
                opts.AutoConnect = false;
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
            client.Connect();

            await client.WaitForState(ConnectionState.Connecting);

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.ProcessCommands();

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

            await client.ProcessCommands();

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

            client.Connection.On((args) =>
            {
                args.Current.Should().Be(ConnectionState.Disconnected);
                args.Previous.Should().Be(ConnectionState.Connecting);
                args.Event.Should().Be(ConnectionEvent.Disconnected);
                args.RetryIn.Should().Be(options.DisconnectedRetryTimeout);
                args.Reason.Should().NotBeNull();
                Done();
            });

            // Let the connecting state complete it's logic otherwise by the time we get to here
            // The transport is not created yet as this is done on a separate thread
            await Task.Delay(1000);

            LastCreatedTransport.Listener.OnTransportEvent(LastCreatedTransport.Id, TransportState.Closing, new Exception());

            WaitOne();
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
                LastCreatedTransport.Listener?.OnTransportEvent(LastCreatedTransport.Id, TransportState.Closing, new Exception());
                await WaitForConnectingOrSuspended(client);
                var now = nowFunc();
                nowFunc = () => now.AddSeconds(30);
            }
            while (client.Connection.State != ConnectionState.Suspended);

            client.Connection.State.Should().Be(ConnectionState.Suspended);

            stateChanges.Select(x => x.Current).Distinct()
                .Should().BeEquivalentTo(new[] { ConnectionState.Connecting, ConnectionState.Disconnected, ConnectionState.Suspended, });
            int numberOfAttemps = (int)Math.Floor(Defaults.ConnectionStateTtl.TotalSeconds / 30);
            stateChanges.Count(x => x.Current == ConnectionState.Connecting).Should().Be(numberOfAttemps);
        }

        [Fact]
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
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(1000);
                opts.NowFunc = NowWrapperFunc;
            });

            client.ExecuteCommand(SetSuspendedStateCommand.Create(ErrorInfo.ReasonSuspended));

            var elapsed = await client.WaitForState(ConnectionState.Connecting);
            elapsed.Should().BeCloseTo(client.Options.SuspendedRetryTimeout, 1000);
        }

        private static Task WaitForConnectingOrSuspended(AblyRealtime client)
        {
            return new ConnectionAwaiter(client.Connection, ConnectionState.Connecting, ConnectionState.Suspended).Wait();
        }

        [Fact]
        [Trait("spec", "RTN14f")]
        public async Task WhenInSuspendedStateAfterRetrying_ShouldGoBackToSuspendedState()
        {
            FakeTransportFactory.InitialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;

            // this will keep it in connecting state
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.AutoConnect = true;
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
            });

            client.ExecuteCommand(SetSuspendedStateCommand.Create(ErrorInfo.ReasonSuspended));

            await client.WaitForState(ConnectionState.Connecting);
            await client.WaitForState(ConnectionState.Suspended);
        }

        public ConnectingFailureSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
