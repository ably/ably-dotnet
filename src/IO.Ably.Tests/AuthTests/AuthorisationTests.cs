using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class AuthorisationTests : MockHttpRestSpecs
    {
        internal override AblyResponse DefaultResponse => DummyTokenResponse;

        internal AblyResponse DummyTokenResponse = new AblyResponse() { Type = ResponseType.Json, TextResponse = "{ \"access_token\": {}}" };

        protected static string KeyId => ValidKey.Split(':')[0];

        private async Task<TokenRequest> CreateTokenRequest(AblyRest client, TokenParams @params = null, AuthOptions options = null)
        {
            return JsonHelper.Deserialize<TokenRequest>(await client.Auth.CreateTokenRequestAsync(@params, options));
        }

        [Fact]
        [Trait("spec", "RSA1")]
        public async Task WithTlsFalseAndBasicAuth_Throws()
        {
            var client = GetRestClient(setOptionsAction: options => { options.Tls = false; });
            await Assert.ThrowsAsync<InsecureRequestException>(() => client.Auth.AuthoriseAsync());
        }

        [Fact]
        [Trait("spec", "RSA1")]
        public async Task WithTlsTrueAndBasicAuth_ShouldWork()
        {
            var client = GetRestClient(setOptionsAction: options => { options.Tls = true; });
            await client.Auth.AuthoriseAsync();

            //Success
        }

        private static TokenRequest CreateDefaultTokenRequest(AblyRest client)
        {
            return JsonHelper.Deserialize<TokenRequest>(client.Auth.CreateTokenRequestAsync(null, null).Result);
        }

        [Fact]
        public void UsesKeyIdFromTheClient()
        {
            var client = GetRestClient();
            var data = CreateDefaultTokenRequest(client);
            data.KeyName.Should().Be(client.Options.ParseKey().KeyName);
        }

        [Fact]
        [Trait("spec", "RSA5")]
        public void UsesDefaultTtlWhenNoneIsSpecified()
        {
            var client = GetRestClient();
            var data = CreateDefaultTokenRequest(client);
            data.Ttl.Should().Be(Defaults.DefaultTokenTtl);
        }

        

        [Fact]
        [Trait("spec", "RSA6")]
        public void UsesTheDefaultCapability()
        {
            var client = GetRestClient();
            var data = CreateDefaultTokenRequest(client);
            data.Capability.Should().Be(Defaults.DefaultTokenCapability);
        }

        [Fact]
        public void UsesUniqueNonseWhichIsMoreThan16Characters()
        {
            var client = GetRestClient();
            var data = CreateDefaultTokenRequest(client);
            var secondTime = CreateDefaultTokenRequest(client);
            data.Nonce.Should().NotBe(secondTime.Nonce);
            data.Nonce.Length.Should().BeGreaterOrEqualTo(16);
        }


        [Trait("spec", "RSA9a")]
        [Trait("spec", "RSA9b")]
        [Trait("spec", "RSA9i")]
        public class CreateTokenRequestAuthOptionSpecs : AuthorisationTests
        {
            public CreateTokenRequestAuthOptionSpecs(ITestOutputHelper output) : base(output)
            {
            }

            private AblyRest GetClientWithTokenParams()
            {
                return GetRestClient(null, options =>
                {
                    options.DefaultTokenParams = new TokenParams()
                    {
                        Ttl = TimeSpan.FromHours(2),
                        Capability = Capability.AllowAll,
                        ClientId = "123",
                        Timestamp = Now.AddMinutes(1),
                        Nonce = "defaultnonce"
                    };
                });
            }

           

            [Fact]
            public async Task WithDefaultTokenParams_ShouldSetTokenRequestValuesCorrectly()
            {
                var client = GetClientWithTokenParams();

                var request = await CreateTokenRequest(client);
                request.Capability.Should().Be(Capability.AllowAll);
                request.ClientId.Should().Be("123");
                request.KeyName.Should().Be(ApiKey.Parse(client.Options.Key).KeyName);
                request.Ttl.Should().Be(TimeSpan.FromHours(2));
                request.Timestamp.Value.Should().BeCloseTo(Now.AddMinutes(1));
                request.Nonce.Should().Be("defaultnonce");
            }

            [Fact]
            public async Task WithOverrideTokenParams_ShouldSetTokenRequestValuesCorrectly()
            {
                var client = GetClientWithTokenParams();

                var overridingTokenParams = new TokenParams()
                {
                    Ttl = TimeSpan.FromHours(1),
                    ClientId = "999",
                    Capability = new Capability(),
                    Nonce = "overrideNonce",
                    Timestamp = Now.AddMinutes(10)
                };

                var request = await CreateTokenRequest(client,overridingTokenParams);

                request.Capability.Should().Be(Capability.Empty);
                request.ClientId.Should().Be("999");
                request.Ttl.Should().Be(TimeSpan.FromHours(1));
                request.Timestamp.Value.Should().BeCloseTo(Now.AddMinutes(10), 200);
                request.Nonce.Should().Be("overrideNonce");
            }

            [Fact]
            public async Task WithOverrideAuthOptions_ShouldSetTokenRequestValuesCorrectly()
            {
                var client = GetClientWithTokenParams();

                var overrideAuthOptions = new AuthOptions()
                {
                    Key = "keyid.name:secret",
                };

                var request = await CreateTokenRequest(client, null, overrideAuthOptions);

                request.KeyName.Should().Be("keyid.name");
            }
        }

        public class CreateTokenRequestSpecs : AuthorisationTests
        {
            [Fact]
            [Trait("spec", "RSA9h")]
            public async Task ReturnsASignedTokenRequest()
            {
                var request = await CreateTokenRequest(Client);
                request.Should().NotBeNull();
                request.Mac.Should().NotBeEmpty();
            }

            [Fact]
            [Trait("spec", "RSA9c")]
            public async Task WithEmptyNonce_ShouldGenerateANonceWithMoreThan16Characters()
            {
                var request = await CreateTokenRequest(Client);
                request.Nonce.Length.Should().BeGreaterThan(16, "Nonce should be more than 16 characters");
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public async Task WithNoTimeStapmInRequest_ShouldUseSystemType()
            {
                var request = await CreateTokenRequest(Client);
                request.Timestamp.Value.Should().BeCloseTo(Now);
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public async Task WithTimeStampOverridesDefault()
            {
                var date = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var data = await CreateTokenRequest(Client, new TokenParams() { Timestamp = date }, null);
                data.Timestamp.Should().Be(date);
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public async Task WithQueryTimeQueriesForTimestamp()
            {
                var currentTime = TestHelpers.Now();
                var client = GetRestClient(x => ("[" + currentTime.ToUnixTimeInMilliseconds() + "]").ToAblyJsonResponse());

                var data = await CreateTokenRequest(client, null, new AuthOptions() { QueryTime = true });
                data.Timestamp.Should().BeCloseTo(currentTime);
            }

            [Fact]
            [Trait("spec", "RSA9e")]
            public async Task WithTtlOverridesDefault()
            {
                var data = await CreateTokenRequest(Client, new TokenParams { Ttl = TimeSpan.FromHours(2) }, null);

                data.Ttl.Should().Be(TimeSpan.FromHours(2));
            }

            [Fact]
            [Trait("spec", "RSA9f")]
            public async Task WithCapabilitySpecifiedInTokenParams_ShouldPassTheJsonStringToRequest()
            {
                var capability = new Capability();
                capability.AddResource("a").AllowAll();
                var customParams = new TokenParams() { Capability = capability };
                var request = await CreateTokenRequest(Client, customParams);
                request.Capability.Should().Be(capability);
            }

            [Fact]
            [Trait("spec", "RSA9g")]
            public async Task GeneratesHMac()
            {
                var data = await CreateTokenRequest(Client);
                data.Mac.Should().NotBeEmpty();
            }

            [Fact]
            public async Task WithNonceOverridesDefault()
            {
                var data = await CreateTokenRequest(Client, new TokenParams() { Nonce = "Blah" }, null);
                data.Nonce.Should().Be("Blah");
            }

            [Fact]
            public async Task WithClientIdOverridesDefault()
            {
                var data = await CreateTokenRequest(Client, new TokenParams() { ClientId = "123" }, null);
                data.ClientId.Should().Be("123");
            }

            [Fact]
            public async Task WithOutKeyIdThrowsException()
            {
                var client = new AblyRest(new ClientOptions());
                await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequestAsync(null, null));
            }

            [Fact]
            public async Task WithOutKeyValueThrowsException()
            {
                var client = new AblyRest(new ClientOptions() { Key = "111.222" });
                await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequestAsync(null, null));
            }

            public CreateTokenRequestSpecs(ITestOutputHelper output) : base(output)
            {
                Client = GetRestClient();
            }

            public AblyRest Client { get; }
        }

        [Fact]
        [Trait("spec", "RSA14")]
        public async Task WithNoTokenOrWayToGenerateOneAndUseTokenAuthIsTrue_AuthoriseShouldThrow()
        {
            var client = GetRestClient(setOptionsAction: options =>
            {
                options.Key = "";
                options.UseTokenAuth = true;
            });

            var ex = await Assert.ThrowsAsync<AblyException>(() => client.Auth.AuthoriseAsync());
            ex.ErrorInfo.Message.Should().Be("TokenAuth is on but there is no way to generate one");
        }

        public class ClientIdSpecs : AuthorisationTests
        {
            private string _clientId = "123";

            public ClientIdSpecs(ITestOutputHelper output) : base(output)
            {

            }

            private AblyRest GetRestClientWithClientId()
            {
                return GetRestClient(null, options => options.ClientId = _clientId);
            }

            [Fact]
            [Trait("spec", "RSA7a1")]
            [Trait("spec", "RSL1g1a")]
            public void ShouldNotPassClientIdWithPublishedMessage()
            {
                var client = GetRestClientWithClientId();

                client.Channels["test"].PublishAsync("boo", "baa");

                var message = (LastRequest.PostData as List<Message>).First();

                message.ClientId.Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RSA7a4")]
            public void WithClientIdandDefaultOptionsParamsClientId_UsesOptionsClientIdWhenMakingTokenRequests()
            {
                var client = GetRestClient(null, options =>
                {
                    options.ClientId = "123";
                    options.DefaultTokenParams = new TokenParams() { ClientId = "999" };
                });

                client.Auth.AuthoriseAsync(null, new AuthOptions() { Force = true});
                var tokenRequest = LastRequest.PostData as TokenRequest;
                tokenRequest.ClientId.Should().Be("123");
            }

            [Fact]
            [Trait("spec", "RSA12b")]
            public void WhenNoClientIdIsSpecified_AuthClientIdShouldBeNull()
            {
                var client = GetRestClient();
                client.AblyAuth.ClientId.Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RSC17")]
            [Trait("spec", "RSA7b1")]
            public void WhenClientIdInOptions_ShouldPassClientIdtoAblyAuth()
            {
                var options = new ClientOptions(ValidKey) { ClientId = "123" };
                var client = new AblyRest(options);
                client.AblyAuth.ClientId.Should().Be(options.ClientId);
            }

            [Fact]
            [Trait("spec", "RSA7b4")]
            public void WhenClientIsInitialisedWithTokenDetails_AuthClientIdShouldBeTheSame()
            {
                var options = new ClientOptions() { TokenDetails = new TokenDetails() { ClientId = "*" } };
                var client = new AblyRest(options);
                client.AblyAuth.ClientId.Should().Be("*");
            }

            [Fact]
            [Trait("spec", "RSA7c")]
            public void ClientIdInClientOptionsCannotBeWildCard()
            {
                Assert.Throws<InvalidOperationException>(() => new ClientOptions() { ClientId = "*" });
            }
        }

        

        

        

        public AuthorisationTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
