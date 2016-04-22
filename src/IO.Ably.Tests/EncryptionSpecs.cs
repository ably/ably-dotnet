using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO.Ably.Encryption;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class EncryptionSpecs : AblySpecs
    {
        public class GetDefaultParamsSpecs : AblySpecs
        {
            [Fact]
            [Trait("spec", "RSE1a")]
            public void ShouldReturnCompleteCipherParamsInstance()
            {
                //Crypto.
            }

            public GetDefaultParamsSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }


        public EncryptionSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}
