using System;
using IO.Ably.Encryption;
using Xunit;

namespace IO.Ably.Tests
{
    public class CipherParamsTests
    {
        [Fact]
        public void Ctor_WithKeyAndNoAlgorithSpecified_DefaultsToAES()
        {
            //Act
            var cipherParams = new CipherParams("", new byte[] {});

            //Assert
            Assert.Equal(Crypto.DefaultAlgorithm, cipherParams.Algorithm);
            Assert.Equal(new Byte[] {}, cipherParams.Key);
        }
    }
}
