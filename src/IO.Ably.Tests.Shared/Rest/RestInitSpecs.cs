using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class RestInitSpecs : AblySpecs
    {
        [Fact]
        [Trait("spec", "RSA2")]
        public void Init_WithKeyAndNoClientId_SetsAuthMethodToBasic()
        {
            var client = new AblyRest(ValidKey);
            Assert.Equal(AuthMethod.Basic, client.AblyAuth.AuthMethod);
        }

        [Trait("spec", "RSA4")]
        [Trait("spec", "RSC14b")]
        public class AuthMethodInitTests
        {
            [Fact]
            public void WithUseTokenAuthSetToTrue_AuthMethodIsAlwaysTokenAuth()
            {
                var client = new AblyRest(new ClientOptions { Key = ValidKey, UseTokenAuth = true });
                client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
            }

            [Fact]
            public void WithKeyAndClientId_ShouldUseTokenAuth()
            {
                var client = new AblyRest(new ClientOptions { Key = ValidKey, ClientId = "123" });
                Assert.Equal(AuthMethod.Token, client.AblyAuth.AuthMethod);
            }

            [Fact]
            public void WithKeyNoClientIdAndAuthToken_ShouldSetCurrentToken()
            {
                ClientOptions options = new ClientOptions { Key = ValidKey, ClientId = "123", Token = "blah"};
                var client = new AblyRest(options);

                client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
                client.AblyAuth.CurrentToken.Token.Should().Be("blah");
            }

            [Fact]
            public void WithouthKey_ShouldUseTokenAuth()
            {
                var client = new AblyRest(opts =>
                {
                    opts.Key = "test.best:rest";
                    opts.Token = "blah";
                    opts.ClientId = "123";
                });

                Assert.Equal(AuthMethod.Token, client.AblyAuth.AuthMethod);
            }

            [Fact]
            public void WithToken_ShouldUseTokenAuth()
            {
                var client = new AblyRest(opts =>
                {
                    opts.Key = "test.best:rest";
                    opts.Token = "blah";
                });

                client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
            }

            [Fact]
            public void WithTokenDetails_ShouldUseTokenAuth()
            {
                var client = new AblyRest(opts =>
                {
                    opts.Key = "test.best:rest";
                    opts.TokenDetails = new TokenDetails("123");
                });

                client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
            }

            [Fact]
            public void WithAuthUrl_ShouldUseTokenAuth()
            {
                var client = new AblyRest(opts =>
                {
                    opts.Key = "test.best:rest";
                    opts.AuthUrl = new Uri("http://authUrl");
                });

                client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
            }

            [Fact]
            public void WithAuthCallback_ShouldUseTokenAuth()
            {
                var client = new AblyRest(opts =>
                {
                    opts.AuthCallback = @params => Task.FromResult<object>(new TokenDetails());
                });

                client.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
            }

            [Fact]
            public void WithTokenOnly_SetsTokenRenewableToFalse()
            {
                var rest = new AblyRest(new ClientOptions() { Token = "token_id" });

                rest.AblyAuth.TokenRenewable.Should().BeFalse();
            }

            [Fact]
            public void WithApiKey_SetsTokenRenewableToTrue()
            {
                var rest = new AblyRest(new ClientOptions(ValidKey));

                rest.AblyAuth.TokenRenewable.Should().BeTrue();
            }

            [Fact]
            public void WithAuthUrl_SetsTokenRenewableToTrue()
            {
                var rest = new AblyRest(new ClientOptions() { AuthUrl = new Uri("http://boo")});

                rest.AblyAuth.TokenRenewable.Should().BeTrue();
            }

            [Fact]
            public void WithAuthCallback_SetsTokenRenewableToTrue()
            {
                var rest = new AblyRest(new ClientOptions() { AuthCallback = token => Task.FromResult<object>(new TokenDetails())});

                rest.AblyAuth.TokenRenewable.Should().BeTrue();
            }
        }

        [Fact]
        public void Init_WithTlsAndSpecificPort_ShouldInitialiseHttpClientWithCorrectPort()
        {
            var client = new AblyRest(opts =>
            {
                opts.Tls = true;
                opts.TlsPort = 111;
            }
                );
            client.HttpClient.Options.Port.Should().Be(111);
        }

        [Fact]
        public void Init_WithTlsFalseAndSpecificPort_ShouldInitialiseHttpClientWithCorrectPort()
        {
            var client = new AblyRest(opts =>
            {
                opts.Tls = false;
                opts.Port = 111;
            }
                );
            client.HttpClient.Options.Port.Should().Be(111);
        }

        public RestInitSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}