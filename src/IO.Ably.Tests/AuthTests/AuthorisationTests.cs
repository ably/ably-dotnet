using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using IO.Ably.Auth;
using Newtonsoft.Json;
using Xunit;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class AuthorisationTests : MockHttpSpecs
    {
        internal override AblyResponse DefaultResponse => DummyTokenResponse;

        internal AblyResponse DummyTokenResponse = new AblyResponse() { Type = ResponseType.Json, TextResponse = "{ \"access_token\": {}}" };

        protected static string KeyId => ValidKey.Split(':')[0];

        [Fact]
        [Trait("spec", "RSA1")]
        public async Task WithTlsFalseAndBasicAuth_Throws()
        {
            var client = GetRestClient(setOptionsAction: options => { options.Tls = false; });
            await Assert.ThrowsAsync<InsecureRequestException>(() => client.Auth.Authorise());
        }

        [Fact]
        [Trait("spec", "RSA1")]
        public async Task WithTlsTrueAndBasicAuth_ShouldWork()
        {
            var client = GetRestClient(setOptionsAction: options => { options.Tls = true; });
            await client.Auth.Authorise();

            //Success
        }

        [Fact]
        public void UsesKeyIdFromTheClient()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            data.KeyName.Should().Be(client.Options.ParseKey().KeyName);
        }

        [Fact]
        [Trait("spec", "RSA5")]
        public void UsesDefaultTtlWhenNoneIsSpecified()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            data.Ttl.Should().Be(Defaults.DefaultTokenTtl.TotalMilliseconds.ToString());
        }

        [Fact]
        [Trait("spec", "RSA6")]
        public void UsesTheDefaultCapability()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            data.Capability.Should().Be(Defaults.DefaultTokenCapability.ToJson());
        }

        [Fact]
        public void UsesUniqueNonseWhichIsMoreThan16Characters()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            var secondTime = client.Auth.CreateTokenRequest(null, null).Result;
            data.Nonce.Should().NotBe(secondTime.Nonce);
            data.Nonce.Length.Should().BeGreaterOrEqualTo(16);
        }


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

                var request = await client.Auth.CreateTokenRequest();
                request.Capability.Should().Be(Capability.AllowAll.ToJson());
                request.ClientId.Should().Be("123");
                request.KeyName.Should().Be(ApiKey.Parse(client.Options.Key).KeyName);
                request.Ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
                request.Timestamp.Should().Be(Now.AddMinutes(1).ToUnixTimeInMilliseconds().ToString());
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

                var request = await client.Auth.CreateTokenRequest(overridingTokenParams);

                request.Capability.Should().Be("");
                request.ClientId.Should().Be("999");
                request.Ttl.Should().Be(TimeSpan.FromHours(1).TotalMilliseconds.ToString());
                request.Timestamp.Should().Be(Now.AddMinutes(10).ToUnixTimeInMilliseconds().ToString());
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

                var request = await client.Auth.CreateTokenRequest(null, overrideAuthOptions);

                request.KeyName.Should().Be("keyid.name");
            }
        }

        public class CreateTokenRequestSpecs : AuthorisationTests
        {
            [Fact]
            [Trait("spec", "RSA9h")]
            public async Task ReturnsASignedTokenRequest()
            {
                var request = await Client.Auth.CreateTokenRequest();
                request.Should().NotBeNull();
                request.Mac.Should().NotBeEmpty();
            }

            [Fact]
            [Trait("spec", "RSA9c")]
            public async Task WithEmptyNonce_ShouldGenerateANonceWithMoreThan16Characters()
            {
                var request =await Client.Auth.CreateTokenRequest();
                request.Nonce.Length.Should().BeGreaterThan(16, "Nonce should be more than 16 characters");
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public async Task WithNoTimeStapmInRequest_ShouldUseSystemType()
            {
                var request = await Client.Auth.CreateTokenRequest();
                request.Timestamp.Should().Be(Now.ToUnixTimeInMilliseconds().ToString());
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public void WithTimeStampOverridesDefault()
            {
                var date = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var data = Client.Auth.CreateTokenRequest(new TokenParams() { Timestamp = date }, null).Result;
                data.Timestamp.Should().Be(date.ToUnixTimeInMilliseconds().ToString());
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public void WithQueryTimeQueriesForTimestamp()
            {
                var currentTime = Config.Now().ToUnixTimeInMilliseconds();
                var client = GetRestClient(x => ("[" + currentTime + "]").ToAblyJsonResponse());

                var data = client.Auth.CreateTokenRequest(null, new AuthOptions() { QueryTime = true }).Result;
                data.Timestamp.Should().Be(currentTime.ToString());
            }

            [Fact]
            [Trait("spec", "RSA9e")]
            public void WithTtlOverridesDefault()
            {
                var data = Client.Auth.CreateTokenRequest(new TokenParams { Ttl = TimeSpan.FromHours(2) }, null).Result;

                data.Ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
            }

            [Fact]
            [Trait("spec", "RSA9f")]
            public async Task WithCapabilitySpecifiedInTokenParams_ShouldPassTheJsonStringToRequest()
            {
                var capability = new Capability();
                capability.AddResource("a").AllowAll();
                var customParams = new TokenParams() { Capability = capability };
                var request = await Client.Auth.CreateTokenRequest(customParams);
                request.Capability.Should().Be(capability.ToJson());
            }

            [Fact]
            [Trait("spec", "RSA9g")]
            public async Task GeneratesHMac()
            {
                var data = await Client.Auth.CreateTokenRequest();
                data.Mac.Should().NotBeEmpty();
            }

            [Fact]
            public void WithNonceOverridesDefault()
            {
                var data = Client.Auth.CreateTokenRequest(new TokenParams() { Nonce = "Blah" }, null).Result;
                data.Nonce.Should().Be("Blah");
            }

            [Fact]
            public void WithClientIdOverridesDefault()
            {
                var data = Client.Auth.CreateTokenRequest(new TokenParams() { ClientId = "123" }, null).Result;
                data.ClientId.Should().Be("123");
            }

            [Fact]
            public async void WithOutKeyIdThrowsException()
            {
                var client = new AblyRest(new ClientOptions());
                await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequest(null, null));
            }

            [Fact]
            public async void WithOutKeyValueThrowsException()
            {
                var client = new AblyRest(new ClientOptions() { Key = "111.222" });
                await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequest(null, null));
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

            var ex = await Assert.ThrowsAsync<AblyException>(() => client.Auth.Authorise());
            ex.ErrorInfo.message.Should().Be("TokenAuth is on but there is no way to generate one");
        }

        public class ClientIdSpecs : AuthorisationTests
        {
            private string _clientId = "123";
            private string _wildcardClientId = "*";

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

                client.Channels["test"].Publish("boo", "baa");

                var message = (LastRequest.PostData as List<Message>).First();

                message.clientId.Should().BeNullOrEmpty();
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

                client.Auth.Authorise(null, new AuthOptions() { Force = true});
                var tokenRequest = LastRequest.PostData as TokenRequest;
                tokenRequest.ClientId.Should().Be("123");
            }

            [Fact]
            [Trait("spec", "RSA12b")]
            public void WhenNoClientIdIsSpecified_AuthClientIdShouldBeNull()
            {
                var client = GetRestClient();
                client.AblyAuth.GetClientId().Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RSC17")]
            [Trait("spec", "RSA7b1")]
            public void WhenClientIdInOptions_ShouldPassClientIdtoAblyAuth()
            {
                var options = new ClientOptions(ValidKey) { ClientId = "123" };
                var client = new AblyRest(options);
                client.AblyAuth.GetClientId().Should().Be(options.ClientId);
            }

            [Fact]
            [Trait("spec", "RSA7b4")]
            public void WhenClientIsInitialisedWithTokenDetails_AuthClientIdShouldBeTheSame()
            {
                var options = new ClientOptions() { TokenDetails = new TokenDetails() { ClientId = "*" } };
                var client = new AblyRest(options);
                client.AblyAuth.GetClientId().Should().Be("*");
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
