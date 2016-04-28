using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.AuthTests
{
    public class AuthoriseTests : AuthorisationTests
    {
        [Fact]
        public void TokenShouldNotBeSetBeforeAuthoriseIsCalled()
        {
            var client = GetRestClient();
            client.AblyAuth.CurrentToken.Should().BeNull();
        }

        [Fact]
        [Trait("spec", "RSA10a")]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetRestClient(null, opts => opts.TokenDetails = new TokenDetails() { Expires = Config.Now().AddHours(1) });

            var token = await client.Auth.Authorise();

            Assert.Same(client.AblyAuth.CurrentToken, token);
        }

        [Fact]
        [Trait("spec", "RSA10a")]
        public async Task Authorise_WithBasicAuthCreatesTokenAndUsesTokenAuthInTheFuture()
        {
            var client = GetRestClient();

            client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Basic);

            await client.Auth.Authorise();

            client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
        }



        [Fact]
        [Trait("spec", "RSA10j")]
        public async Task Authorise_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetRestClient();
            var tokenParams = new TokenParams() { Ttl = TimeSpan.FromMinutes(260) };
            await client.Auth.Authorise(tokenParams, null);
            await client.Auth.Authorise(null, new AuthOptions() { Force = true});
            var data = LastRequest.PostData as TokenRequest;
            client.AblyAuth.CurrentTokenParams.ShouldBeEquivalentTo(tokenParams);
            data.Ttl.Should().Be(TimeSpan.FromMinutes(260).TotalMilliseconds.ToString());
        }

        [Fact]
        [Trait("spec", "RSA10d")]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            var initialToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };
            client.AblyAuth.CurrentToken = initialToken;

            var token = await client.Auth.Authorise(new TokenParams() { ClientId = "123", Capability = new Capability() }, new AuthOptions { Force = true});

            Assert.Contains("requestToken", LastRequest.Url);
            token.Should().NotBeSameAs(initialToken);
        }

        [Fact]
        [Trait("spec", "RSA10c")]
        public async Task Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetRestClient();
            var initialToken = new TokenDetails() { Expires = Config.Now().AddHours(-1) };
            client.AblyAuth.CurrentToken = initialToken;

            var token = await client.Auth.Authorise();
            ;
            Assert.Contains("requestToken", LastRequest.Url);
            token.Should().NotBeSameAs(initialToken);
        }

        [Theory]
        [InlineData(Defaults.TokenExpireBufferInSeconds + 1, false)]
        [InlineData(Defaults.TokenExpireBufferInSeconds, true)]
        [InlineData(Defaults.TokenExpireBufferInSeconds - 1, true)]
        [Trait("spec", "RSA10c")]
        public async Task Authorise_WithTokenExpiringIn15Seconds_RenewsToken(int secondsLeftToExpire, bool shouldRenew)
        {
            var client = GetRestClient();
            var initialToken = new TokenDetails() { Expires = Now.AddSeconds(secondsLeftToExpire) };
            client.AblyAuth.CurrentToken = initialToken;

            var token = await client.Auth.Authorise();

            if (shouldRenew)
            {
                Assert.Contains("requestToken", LastRequest.Url);
                token.Should().NotBeSameAs(initialToken);
            }
            else
            {
                token.Should().BeSameAs(initialToken);
            }
        }

        [Fact]
        [Trait("spec", "RSA10g")]
        public async Task ShouldKeepTokenParamsAndAuthOptionsExcetpForceAndCurrentTimestamp()
        {
            var client = GetRestClient();
            var testAblyAuth = new TestAblyAuth(client.Options, client);
            var customTokenParams = new TokenParams() { Ttl = TimeSpan.FromHours(2), Timestamp = Now.AddHours(1)};
            var customAuthOptions = new AuthOptions() { UseTokenAuth = true, Force = true };

            await testAblyAuth.Authorise(customTokenParams, customAuthOptions);
            var expectedTokenParams = customTokenParams.Clone();
            expectedTokenParams.Timestamp = null;
            testAblyAuth.CurrentTokenParams.ShouldBeEquivalentTo(expectedTokenParams);

            testAblyAuth.CurrentAuthOptions.Should().BeSameAs(customAuthOptions);
            testAblyAuth.CurrentTokenParams.Timestamp.Should().Be(null);
            testAblyAuth.CurrentAuthOptions.Force.Should().BeFalse();
        }

        [Fact]
        [Trait("spec", "RSA10g")]
        public async Task ShouldKeepCurrentTokenParamsAndOptionsEvenIfCurrentTokenIsValidAndNoNewTokenIsRequested()
        {
            var client = GetRestClient(null,
                opts => opts.TokenDetails = new TokenDetails("boo") {Expires = Now.AddHours(10)});

            var testAblyAuth = new TestAblyAuth(client.Options, client);
            var customTokenParams = new TokenParams() { Ttl = TimeSpan.FromHours(2), Timestamp = Now.AddHours(1) };
            var customAuthOptions = new AuthOptions() { UseTokenAuth = true };

            await testAblyAuth.Authorise(customTokenParams, customAuthOptions);
            var expectedTokenParams = customTokenParams.Clone();
            expectedTokenParams.Timestamp = null;
            testAblyAuth.CurrentTokenParams.ShouldBeEquivalentTo(expectedTokenParams);
            testAblyAuth.CurrentAuthOptions.Should().BeSameAs(customAuthOptions);
            testAblyAuth.CurrentAuthOptions.Force.Should().BeFalse();
        }

        //This Test delegate all the work to RequestToken which has tests coving the following spec items
        [Fact]
        [Trait("spec", "RSA10b")]
        [Trait("spec", "RSA10e")]
        [Trait("spec", "RSA10h")]
        [Trait("spec", "RSA10i")]
        public async Task AuthoriseUseRequestTokenToCreateTokensAndPassesTokenParamsAndAuthOptions()
        {
            var client = GetRestClient();
            var testAblyAuth = new TestAblyAuth(client.Options, client);
            var customTokenParams = new TokenParams() { Ttl = TimeSpan.FromHours(2) };
            var customAuthOptions = new AuthOptions() { UseTokenAuth = true };

            await testAblyAuth.Authorise(customTokenParams, customAuthOptions);

            testAblyAuth.RequestTokenCalled.Should().BeTrue("Token creation was not delegated to RequestToken");
            testAblyAuth.LastRequestTokenParams.Should().BeSameAs(customTokenParams);
            testAblyAuth.LastRequestAuthOptions.Should().BeSameAs(customAuthOptions);
        }

        class TestAblyAuth : AblyAuth
        {
            public bool RequestTokenCalled { get; set; }
            public TokenParams LastRequestTokenParams { get; set; }
            public AuthOptions LastRequestAuthOptions { get; set; }
            public override Task<TokenDetails> RequestToken(TokenParams tokenParams, AuthOptions options)
            {
                RequestTokenCalled = true;
                LastRequestTokenParams = tokenParams;
                LastRequestAuthOptions = options;

                return base.RequestToken(tokenParams, options);
            }

            public TestAblyAuth(ClientOptions options, AblyRest rest) : base(options, rest)
            {
            }
        }

        public AuthoriseTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
