using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN15")]
    public class ConnectionFailuresOnceConnectedSpecs : ConnectionSpecsBase
    {
        private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = Config.Now().AddDays(1), ClientId = "123" };
        private int _tokenErrorCode = 40140;
        private bool _renewTokenCalled;
        private TokenDetails _validToken;
        private ErrorInfo _tokenErrorInfo;
        private int _failedRenewalErorrCode = 1234;

        public AblyRealtime SetupConnectedClient(bool failRenewal = false, bool renewable = true)
        {
            return GetConnectedClient(opts =>
            {
                if (renewable == false) opts.Key = ""; //clear the key to make the token non renewable
                opts.TokenDetails = _validToken;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    if (failRenewal)
                        throw new AblyException(new ErrorInfo("Failed to renew token", _failedRenewalErorrCode));
                    _renewTokenCalled = true;
                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WithDisconnectMessageWithTokenError_ShouldRenewTokenAndReconnect()
        {
            var client = SetupConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
                if (args.CurrentState == ConnectionStateType.Connecting)
                {
                    client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };
            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { error = _tokenErrorInfo });

            _renewTokenCalled.Should().BeTrue();
            Assert.Equal(new[] { ConnectionStateType.Disconnected, ConnectionStateType.Connecting, ConnectionStateType.Connected }, states);
            errors.Should().BeEmpty("There should be no errors emitted by the client");

            var currentToken = client.Auth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WithTokenErrorWhenTokenRenewalFails_ShouldGoToFailedStateAndEmitError()
        {
            var client = SetupConnectedClient(failRenewal: true);

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
                {
                    error = _tokenErrorInfo
                });

            Assert.Equal(new[]
            {
                ConnectionStateType.Disconnected,
                ConnectionStateType.Connecting,
                ConnectionStateType.Failed
            }, states);

            errors.Should().NotBeEmpty();
            errors.First().code.Should().Be(_failedRenewalErorrCode);
        }

        [Fact]
        [Trait("spec", "RTN15f")]
        public async Task WhenConnectionFailsConsecutivelyMoreThanOnceWithTokenError_ShouldTransitionToFailedWithError()
        {
            var client = SetupConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                error = _tokenErrorInfo
            });

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = _tokenErrorInfo
            });

            Assert.Equal(new[]
            {
                ConnectionStateType.Disconnected,
                ConnectionStateType.Connecting,
                ConnectionStateType.Failed
            }, states);

            errors.Should().NotBeEmpty();
            errors.First().code.Should().Be(_tokenErrorInfo.code);
        }

        [Fact]
        [Trait("spec", "RTN15f")]
        public async Task WhenConnectionFailsWithTokenErrorButTokenIsNotRenewable_ShouldTransitionDirectlyToFailedWithError()
        {
            var client = SetupConnectedClient(renewable: false);

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                error = _tokenErrorInfo
            });

            Assert.Equal(new[]
            {
                ConnectionStateType.Failed
            }, states);

            errors.Should().NotBeEmpty();
        }



        public ConnectionFailuresOnceConnectedSpecs(ITestOutputHelper output) : base(output)
        {
            Now = DateTimeOffset.Now;
            _validToken = new TokenDetails("id") { Expires = Now.AddHours(1) };
            _renewTokenCalled = false;
            _tokenErrorInfo = new ErrorInfo() { code = _tokenErrorCode, statusCode = HttpStatusCode.Unauthorized };
        }
    }
}