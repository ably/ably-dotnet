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
            Assert.Throws<ConfigurationMissingException>(delegate {
             new RestThatReadsDummyConnectionString();
            });
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
            Assert.Throws<AblyInvalidApiKeyException>(delegate
            {
                new Rest("InvalidKey");
            });
        }
    }
}
