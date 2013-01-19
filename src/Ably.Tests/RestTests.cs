using Ably.Tests;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.Serialization;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class RestTests
    {
        private const string ValidKey = "AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob";
        private readonly ApiKey Key = ApiKey.Parse(ValidKey);

        private class RestThatReadsDummyConnectionString : Rest
        {
            internal override string GetConnectionString()
            {
                return "";
            }
        }

        [Fact]
        public void Ctor_WithNoParametersAndNoAblyConnectionString_Throws()
        {
            var ex = Assert.Throws<AblyException>(delegate {
             new RestThatReadsDummyConnectionString();
            });

            Assert.IsType<ConfigurationMissingException>(ex.InnerException);
        }

        [Fact]
        public void Ctor_WithNoParametersAndAblyConnectionString_RetrievesApiKeyFromConnectionString()
        {
            var rest = new Rest();

            Assert.NotNull(rest);
        }

        [Fact]
        public void Ctor_WithNoParametersWithInvalidKey_ThrowsInvalidKeyException()
        {
            AblyException ex = Assert.Throws<AblyException>(delegate
            {
                new Rest("InvalidKey");
            });

            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        }

        [Fact]
        public void Ctor_WithKeyPassedInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithKeyInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithAppIdInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.AppId = Key.AppId);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithNoAppIdOrKey_Throws()
        {
            var ex = Assert.Throws<AblyException>(delegate { new Rest(""); });

            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void ChannelsGet_ReturnsNewChannelWithName()
        {
            var rest = new Rest(ValidKey);

            var channel = rest.Channels.Get("Test");

            Assert.Equal("Test", channel.Name);
        }
    }
}
