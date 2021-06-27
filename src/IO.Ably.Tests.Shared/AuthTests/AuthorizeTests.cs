using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.AuthTests
{
    [Collection("UnitTests")]
    public class ServerTimeTests : AuthorizationTests
    {
        [Fact]
        [Trait("spec", "RSA10k")]
        public async Task Authorize_WillObtainServerTimeAndPersistTheOffsetFromTheLocalClock()
        {
            var client = GetRestClient();
            bool serverTimeCalled = false;

            // configure the AblyAuth test wrapper to return UTC+30m when ServerTime() is called
            // (By default the library uses DateTimeOffset.UtcNow whe Now() is called)
            var testAblyAuth = new TestAblyAuth(client.Options, client, () =>
            {
                serverTimeCalled = true;
                return Task.FromResult(DateTimeOffset.UtcNow.AddMinutes(30));
            });

            // RSA10k: If the AuthOption argument’s queryTime attribute is true
            // it will obtain the server time once and persist the offset from the local clock.
            var tokenParams = new TokenParams();
            testAblyAuth.Options.QueryTime = true;
            await testAblyAuth.AuthorizeAsync(tokenParams);
            serverTimeCalled.Should().BeTrue();
            testAblyAuth.GetServerNow().Should().HaveValue();
            testAblyAuth.GetServerNow()?.Should().BeCloseTo(await testAblyAuth.GetServerTime(), precision: 1000); // Allow 1s clock skew
            testAblyAuth.GetServerNow()?.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(30), precision: 1000); // Allow 1s clock skew
        }

        [Fact]
        [Trait("spec", "RSA10k")]
        public async Task Authorize_WillObtainServerTimeAndPersist_ShouldShowValuesAreCalculated()
        {
            var client = GetRestClient();

            // configure the AblyAuth test wrapper to return UTC+30m when ServerTime() is called
            // (By default the library uses DateTimeOffset.UtcNow whe Now() is called)
            var testAblyAuth = new TestAblyAuth(client.Options, client, () => Task.FromResult(DateTimeOffset.UtcNow.AddMinutes(30)));

            // RSA10k: If the AuthOption argument’s queryTime attribute is true
            // it will obtain the server time once and persist the offset from the local clock.
            testAblyAuth.Options.QueryTime = true;
            var tokenParams = new TokenParams();
            testAblyAuth.Options.QueryTime = true;
            await testAblyAuth.AuthorizeAsync(tokenParams);

            // to show the values are calculated and not fixed
            // get the current server time offset, pause for a short time,
            // then get it again.
            // The new value should represent a time after the first
            var snapshot = testAblyAuth.GetServerNow();
            await Task.Delay(500);
            testAblyAuth.GetServerNow()?.Should().BeAfter(snapshot.Value);
        }

        [Fact]
        [Trait("spec", "RSA10k")]
        public async Task Authorize_WillObtainServerTimeAndPersist_ShouldNotUserServerTimeWhenAuthObjectIsReset()
        {
            var client = GetRestClient();
            var serverTimeCalled = 0;

            // configure the AblyAuth test wrapper to return UTC+30m when ServerTime() is called
            // (By default the library uses DateTimeOffset.UtcNow whe Now() is called)
            var testAblyAuth = new TestAblyAuth(client.Options, client, () =>
            {
                Interlocked.Increment(ref serverTimeCalled);
                return Task.FromResult(DateTimeOffset.UtcNow.AddMinutes(30));
            });

            // RSA10k: If the AuthOption argument’s queryTime attribute is true
            // it will obtain the server time once and persist the offset from the local clock.
            testAblyAuth.Options.QueryTime = true;
            var tokenParams = new TokenParams();
            testAblyAuth.Options.QueryTime = true;
            await testAblyAuth.AuthorizeAsync(tokenParams);

            // intercept the (mocked) HttpRequest so we can get a reference to the AblyRequest
            TokenRequest tokenRequest = null;
            var exFunc = client.ExecuteHttpRequest;
            client.ExecuteHttpRequest = request =>
            {
                tokenRequest = request.PostData as TokenRequest;
                return exFunc(request);
            };

            // reset auth object
            testAblyAuth = new TestAblyAuth(client.Options, client, () => Task.FromResult(DateTimeOffset.UtcNow.AddDays(1)));
            testAblyAuth.Options.QueryTime = false;
            await testAblyAuth.AuthorizeAsync();

            // the TokenRequest should not have been set using an offset, but should have been set
            tokenRequest.Timestamp.Should().HaveValue();
            tokenRequest.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, 1000);
        }

        [Fact]
        [Trait("spec", "RSA10k")]
        public async Task Authorize_WillObtainServerTimeAndPersist_ShouldUserServerTimeEvenIfFurtherRequestsDontHaveQueryTimeSetToTrue()
        {
            var client = GetRestClient();
            var serverTimeCalled = 0;

            // configure the AblyAuth test wrapper to return UTC+30m when ServerTime() is called
            // (By default the library uses DateTimeOffset.UtcNow whe Now() is called)
            var testAblyAuth = new TestAblyAuth(client.Options, client, () =>
            {
                Interlocked.Increment(ref serverTimeCalled);
                return Task.FromResult(DateTimeOffset.UtcNow.AddMinutes(30));
            });

            // RSA10k: If the AuthOption argument’s queryTime attribute is true
            // it will obtain the server time once and persist the offset from the local clock.
            testAblyAuth.Options.QueryTime = true;
            var tokenParams = new TokenParams();
            testAblyAuth.Options.QueryTime = true;
            await testAblyAuth.AuthorizeAsync(tokenParams);

            // intercept the (mocked) HttpRequest so we can get a reference to the AblyRequest
            TokenRequest tokenRequest = null;
            var exFunc = client.ExecuteHttpRequest;
            client.ExecuteHttpRequest = request =>
            {
                tokenRequest = request.PostData as TokenRequest;
                return exFunc(request);
            };

            // demonstrate that we don't need Querytime set to get a server time offset
            testAblyAuth.Options.QueryTime = false;
            await testAblyAuth.AuthorizeAsync(tokenParams);

            // offset should be cached
            serverTimeCalled.Should().Be(1);

            // the TokenRequest timestamp should have been set using the offset
            tokenRequest.Timestamp.Should().HaveValue();
            tokenRequest.Timestamp.Should().BeCloseTo(await testAblyAuth.GetServerTime(), 1000);
        }

        [Fact]
        [Trait("spec", "RSA10k")]
        public async Task Authorize_ObtainServerTimeAndPersistOffset_ShouldShowServerTimeIsCalledOnlyOnce()
        {
            var client = GetRestClient();
            bool serverTimeCalled = false;

            // configure the AblyAuth test wrapper to return UTC+30m when ServerTime() is called
            // (By default the library uses DateTimeOffset.UtcNow whe Now() is called)
            var testAblyAuth = new TestAblyAuth(client.Options, client, () =>
            {
                serverTimeCalled = true;
                return Task.FromResult(DateTimeOffset.UtcNow.AddMinutes(30));
            });

            // RSA10k: If the AuthOption argument’s queryTime attribute is true
            // it will obtain the server time once and persist the offset from the local clock.
            var tokenParams = new TokenParams();
            testAblyAuth.Options.QueryTime = true;
            await testAblyAuth.AuthorizeAsync(tokenParams);

            // to show the values are calculated and not fixed
            // get the current server time offset, pause for a short time,
            // then get it again.
            // The new value should represent a time after the first
            testAblyAuth.GetServerNow();

            // reset flag, used to show ServerTime() is not called again
            serverTimeCalled = false;

            // RSA10k: All future token requests generated directly or indirectly via a call to
            // authorize will not obtain the server time, but instead use the local clock
            // offset to calculate the server time.
            await testAblyAuth.AuthorizeAsync();

            // ServerTime() should not have been called again
            serverTimeCalled.Should().BeFalse();

            // and we should still be getting calculated offsets
            testAblyAuth.GetServerNow().Should().HaveValue();
            testAblyAuth.GetServerNow()?.Should().BeCloseTo(await testAblyAuth.GetServerTime(), 1000);
            testAblyAuth.GetServerNow()?.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(30), 1000);
        }

        public ServerTimeTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }

    [Collection("UnitTests")]
    public class AuthorizeTests : AuthorizationTests
    {
        [Fact]
        public void TokenShouldNotBeSetBeforeAuthorizeIsCalled()
        {
            var client = GetRestClient();
            client.AblyAuth.CurrentToken.Should().BeNull();
        }

        [Fact]
        [Trait("spec", "RSA10a")]
        [Trait("spec", "RSA10f")]
        public async Task Authorize_WithBasicAuthCreatesTokenAndUsesTokenAuthInTheFuture()
        {
            var client = GetRestClient();
            client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Basic);
            var tokenDetails = await client.Auth.AuthorizeAsync();
            tokenDetails.Should().NotBeNull();
            client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
        }

        [Fact]
        [Trait("spec", "RSA10j")]
        public async Task Authorize_TokenParamsAndAuthOptionsReplaceConfiguredDefaults()
        {
            var rest = GetRestClient();

            var capabilityString = "{\"cansubscribe:*\":[\"subscribe\"]}";
            var fakeApiKey = "foo.bar:baz";
            var cap = new Capability(capabilityString);

            var tokenParams = new TokenParams { Capability = cap };

            var token = new TokenDetails();
            var authOptions = new AuthOptions(fakeApiKey);
            var result = await rest.AblyAuth.AuthorizeAsync(tokenParams, authOptions);

            var tokenRequest = Requests[0].PostData as TokenRequest;
            tokenRequest.Capability.Should().Be(cap);
            fakeApiKey.Should().StartWith(tokenRequest.KeyName);

            rest.AblyAuth.CurrentAuthOptions.Should().BeEquivalentTo(authOptions);
            rest.AblyAuth.CurrentTokenParams.Should().BeEquivalentTo(tokenParams);
        }

        [Fact]
        [Trait("spec", "RSA10j")]
        public async Task Authorize_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetRestClient();
            var tokenParams = TokenParams.WithDefaultsApplied();
            tokenParams.Ttl = TimeSpan.FromMinutes(260);
            await client.Auth.AuthorizeAsync(tokenParams);
            await client.Auth.AuthorizeAsync();
            var data = LastRequest.PostData as TokenRequest;
            client.AblyAuth.CurrentTokenParams.Should().BeEquivalentTo(tokenParams);
            data.Ttl.Should().Be(TimeSpan.FromMinutes(260));
        }

        [Fact]
        [Trait("spec", "RSA10g")]
        public async Task ShouldKeepTokenParamsAndAuthOptionsExcetpForceAndCurrentTimestamp()
        {
            var client = GetRestClient();
            var testAblyAuth = new TestAblyAuth(client.Options, client);
            var customTokenParams = TokenParams.WithDefaultsApplied();
            customTokenParams.Merge(new TokenParams { Ttl = TimeSpan.FromHours(2), Timestamp = Now.AddHours(1) });
            var customAuthOptions = AuthOptions.FromExisting(testAblyAuth.Options);
            customAuthOptions.UseTokenAuth = true;

            await testAblyAuth.AuthorizeAsync(customTokenParams, customAuthOptions);
            var expectedTokenParams = customTokenParams.Clone();
            expectedTokenParams.Timestamp = null;
            testAblyAuth.CurrentTokenParams.Should().BeEquivalentTo(expectedTokenParams);

            testAblyAuth.CurrentAuthOptions.Should().BeSameAs(customAuthOptions);
            testAblyAuth.CurrentTokenParams.Timestamp.Should().Be(null);
        }

        [Fact]
        [Trait("spec", "RSA10g")]
        public async Task ShouldKeepCurrentTokenParamsAndOptionsEvenIfCurrentTokenIsValidAndNoNewTokenIsRequested()
        {
            var client = GetRestClient(
                null,
                opts => opts.TokenDetails = new TokenDetails("boo") { Expires = Now.AddHours(10) });

            var testAblyAuth = new TestAblyAuth(client.Options, client);
            var customTokenParams = TokenParams.WithDefaultsApplied();
            customTokenParams.Ttl = TimeSpan.FromHours(2);
            customTokenParams.Timestamp = Now.AddHours(1);

            var customAuthOptions = AuthOptions.FromExisting(testAblyAuth.Options);
            customAuthOptions.UseTokenAuth = true;

            await testAblyAuth.AuthorizeAsync(customTokenParams, customAuthOptions);
            var expectedTokenParams = customTokenParams.Clone();
            expectedTokenParams.Timestamp = null;
            testAblyAuth.CurrentTokenParams.Should().BeEquivalentTo(expectedTokenParams);
            testAblyAuth.CurrentAuthOptions.Should().BeSameAs(customAuthOptions);
        }

        [Fact]
        [Trait("spec", "RSA10b")]
        [Trait("spec", "RSA10e")]
        [Trait("spec", "RSA10h")]
        [Trait("spec", "RSA10i")]
        public async Task Authorize_UseRequestTokenToCreateTokensAndPassesTokenParamsAndAuthOptions()
        {
            var client = GetRestClient();
            var testAblyAuth = new TestAblyAuth(client.Options, client);
            testAblyAuth.Options.UseTokenAuth = true;
            var customAuthOptions = testAblyAuth.Options;
            var customTokenParams = TokenParams.WithDefaultsApplied();
            customTokenParams.Ttl = TimeSpan.FromHours(2);

            await testAblyAuth.AuthorizeAsync(customTokenParams, customAuthOptions);

            testAblyAuth.RequestTokenCalled.Should().BeTrue("Token creation was not delegated to RequestToken");
            testAblyAuth.LastRequestTokenParams.Should().BeSameAs(customTokenParams);
            testAblyAuth.LastRequestAuthOptions.Should().BeSameAs(customAuthOptions);
        }

        [Fact]
        [Trait("spec", "RSA10l")]
        public async Task Authorize_RestClientAuthoriseMethodsShouldBeMarkedObsoleteAndLogADeprecationWarning()
        {
            /* Check for Obsolete Attribute  */
            MethodBase method = typeof(AblyAuth).GetMethod("Authorise");
            method.Should().NotBeNull();
            var attr = (ObsoleteAttribute)method?.GetCustomAttribute(typeof(ObsoleteAttribute));
            attr.Should().NotBeNull();

            method = typeof(AblyAuth).GetMethod("AuthoriseAsync");
            method.Should().NotBeNull();
            attr = (ObsoleteAttribute)method?.GetCustomAttribute(typeof(ObsoleteAttribute));
            attr.Should().NotBeNull();

            method = typeof(AblyAuth).GetMethod("Authorize");
            method.Should().NotBeNull();
            attr = (ObsoleteAttribute)method?.GetCustomAttribute(typeof(ObsoleteAttribute));
            attr.Should().BeNull();

            method = typeof(AblyAuth).GetMethod("AuthorizeAsync");
            method.Should().NotBeNull();
            attr = (ObsoleteAttribute)method?.GetCustomAttribute(typeof(ObsoleteAttribute));
            attr.Should().BeNull();

#pragma warning disable CS0618 // Type or member is obsolete
            /* Check for logged warning */
            var testLogger1 = new TestLogger("AuthoriseAsync is deprecated and will be removed in the future, please replace with a call to AuthorizeAsync");
            var client = GetRestClient(setOptionsAction: options => { options.Logger = testLogger1; });
            var testAblyAuth = new TestAblyAuth(client.Options, client);
            await testAblyAuth.AuthoriseAsync();
            testLogger1.MessageSeen.Should().BeTrue();

            var testLogger2 = new TestLogger("Authorise is deprecated and will be removed in the future, please replace with a call to Authorize");
            client = GetRestClient(setOptionsAction: options => { options.Logger = testLogger2; });
            testAblyAuth = new TestAblyAuth(client.Options, client);
            testAblyAuth.Authorise();
            testLogger2.MessageSeen.Should().BeTrue();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        [Trait("bug", "346")]
        public async Task Issue336_SetServerTimeExceptionsShouldBeHandled()
        {
            /*
             * This is a test to demonstrate the fix for issue
             * https://github.com/ably/ably-dotnet/issues/346
             * against the 1.1.6 release this test would fail
             */

            var client = GetRestClient();
            bool serverTimeCalled = false;

            // configure serverTime delegate to throw an exception
            var testAblyAuth = new TestAblyAuth(client.Options, client, () =>
                {
                    serverTimeCalled = true;
                    throw new Exception("baz");
                });

            var tokenParams = new TokenParams();
            testAblyAuth.Options.QueryTime = true;

            Exception exception = null;
            try
            {
                await testAblyAuth.RequestTokenAsync(tokenParams, client.Options);
            }
            catch (Exception e)
            {
                exception = e;
            }

            serverTimeCalled.Should().BeTrue();
            exception.Should().NotBeNull();
        }

        public AuthorizeTests(ITestOutputHelper helper)
            : base(helper) { }
    }
}
