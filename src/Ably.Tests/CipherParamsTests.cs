using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class DataTests
    {
        [Theory]
        [InlineData("Test")]
        [InlineData(new byte[] { 1, 3, 5})]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(long.MaxValue)]
        [InlineData(20.1)]
        public void ConvertsToTextAndBackCorrectly(object source)
        {
            var text = Data.AsPlaintext(source);
            var obj = Data.FromPlaintext(text);
            Assert.Equal(source, obj);
        }

        [Fact]
        public void Converts_JsonObjectBackAndForthCorrectly()
        {
            //Arrange
            var payload = new MessagePayload() {Name = "Test", Data = "1234"};
            var jPayload = JObject.FromObject(payload);

            //Act
            var text = Data.AsPlaintext(jPayload);
            var obj = Data.FromPlaintext(text);


            //Assert
            Assert.Equal(jPayload.ToString(), obj.ToString());
        }

        [Fact]
        public void Converts_JsonArrayBackAndFortheCorrectly()
        {
            //Arrange
            var payload = new List<MessagePayload>
            {
                new MessagePayload() {Name = "Test", Data = "1234"},
                new MessagePayload() {Name = "Test1", Data = "12345"},
            };

            var jPayload = JArray.FromObject(payload);

            //Act
            var text = Data.AsPlaintext(jPayload);
            var obj = Data.FromPlaintext(text);


            //Assert
            Assert.Equal(jPayload.ToString(), obj.ToString());
        }
    }

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
