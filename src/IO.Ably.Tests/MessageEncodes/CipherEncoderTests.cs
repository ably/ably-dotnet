using System.Reflection;
using System.Security.Cryptography;
using IO.Ably;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.MessageEncoders;
using IO.Ably.Platform;
using IO.Ably.Rest;
using Xunit;

namespace IO.Ably.Tests.MessageEncodes
{
    public class CipherEncoderTests
    {
        private string _stringData;
        private byte[] _binaryData = { 2, 3, 4, 5, 6 };
        private byte[] _encryptedBinaryData;
        private byte[] _key;
        private CipherEncoder encoder;
        private ChannelOptions _channelOptions;
        private byte[] _encryptedData;
        private IChannelCipher _crypto;

        public CipherEncoderTests(int keyLength = Crypto.DefaultKeylength, bool encrypt = false)
        {
            _stringData = "random-string";
            _key = GenerateKey(keyLength);
            _channelOptions =
                new ChannelOptions(encrypt, new CipherParams(Crypto.DefaultAlgorithm, _key, Encryption.CipherMode.CBC));
            _crypto = Crypto.GetCipher(_channelOptions.CipherParams);
            _encryptedData = _crypto.Encrypt(_stringData.GetBytes());
            _encryptedBinaryData = _crypto.Encrypt(_binaryData);

            encoder = new CipherEncoder(Protocol.MsgPack);
        }

        private byte[] GenerateKey(int keyLength)
        {
            var keyGen = new Rfc2898DeriveBytes("password", 8);
            return keyGen.GetBytes(keyLength / 8);
        }

        public class WithInvalidChannelOptions
        {
            [Fact]
            public void WithInvalidKeyLength_Throws()
            {
                var options = new ChannelOptions(new CipherParams(Crypto.DefaultAlgorithm, new byte[] {1,2,3 }));
                var encoder = new CipherEncoder(Protocol.MsgPack);
                var error = Assert.Throws<AblyException>(delegate
                {
                    encoder.Encode(new Message() { data = "string" }, options);
                });

                error.InnerException.Should().BeOfType<CryptographicException>();
            }

            [Fact]
            public void WithInvalidKey_Throws()
            {
                var options = new ChannelOptions(new CipherParams(Crypto.DefaultAlgorithm, new byte[] { 1, 2, 3 }));
                var encoder = new CipherEncoder(Protocol.MsgPack);
                var error = Assert.Throws<AblyException>(delegate
                {
                    encoder.Encode(new Message() { data = "string" }, options);
                });

                error.InnerException.Should().BeOfType<CryptographicException>();
            }

            [Fact]
            public void WithInvalidAlgorithm_Throws()
            {
                var keyGen = new Rfc2898DeriveBytes("password", 8);
                var key = keyGen.GetBytes(Crypto.DefaultKeylength / 8);

                var options = new ChannelOptions(new CipherParams("mgg", key));
                var encoder = new CipherEncoder(Protocol.MsgPack);
                var error = Assert.Throws<AblyException>(delegate
                {
                    encoder.Encode(new Message() { data = "string" }, options);
                });

                error.Message.Should().Contain("Currently only the AES encryption algorithm is supported");
            }
        }

        public class EncodeWith256CBCCipherParams : CipherEncoderTests
        {
            public EncodeWith256CBCCipherParams()
                : base(keyLength: 256, encrypt: true)
            {

            }

            [Fact]
            public void WithStringData_EncryptsDataAndSetsCorrectEncoding()
            {
                var payload = new Message() { data = _stringData };

                encoder.Encode(payload, _channelOptions);

                var result =
                     _crypto.Decrypt(payload.data as byte[]).GetText();

                result.Should().Be(_stringData);

                payload.encoding.Should().Be("utf-8/cipher+aes-256-cbc");
            }
        }



        public class EncodeWithDefaultCipherParams : CipherEncoderTests
        {
            public EncodeWithDefaultCipherParams()
                : base(encrypt: true)
            {

            }

            [Fact]
            public void WithStringData_EncryptsTheDataAndAddsEncodingAndExtraUtf8()
            {
                var payload = new Message() { data = _stringData };

                encoder.Encode(payload, _channelOptions);

                string result = _crypto.Decrypt((byte[])payload.data).GetText();
                result.Should().Be(_stringData);
                payload.encoding.Should().Be("utf-8/cipher+aes-256-cbc");
            }

            [Fact]
            public void WithBinaryData_EncryptsTheDataAndAddsCorrectEncoding()
            {
                var payload = new Message() { data = _binaryData };

                encoder.Encode(payload, _channelOptions);

                byte[] result = _crypto.Decrypt((byte[])payload.data);
                result.Should().BeEquivalentTo(_binaryData);
                payload.encoding.Should().Be("cipher+aes-256-cbc");
            }

            [Fact]
            public void WithJsonData_EncryptsTheDataAndAddsCorrectEncodings()
            {
                var payload = new Message() { data = _stringData, encoding = "json" };

                encoder.Encode(payload, _channelOptions);

                string result = _crypto.Decrypt((byte[])payload.data).GetText();
                result.Should().BeEquivalentTo(_stringData);
                payload.encoding.Should().Be("json/utf-8/cipher+aes-256-cbc");
            }

            [Fact]
            public void WithAlreadyEncryptedData_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() { data = _encryptedData, encoding = "utf-8/cipher+aes-256-cbc" };

                encoder.Encode(payload, _channelOptions);

                payload.data.Should().BeSameAs(_encryptedData);
                payload.encoding.Should().Be("utf-8/cipher+aes-256-cbc");
            }

        }

        public class DecodeWithDefaultCipherParams : CipherEncoderTests
        {
            public DecodeWithDefaultCipherParams() : base(encrypt: true)
            {

            }

            [Fact]
            public void WithCipherPayload_DercyptsDataAndStripsEncoding()
            {
                var payload = new Message() { data = _encryptedBinaryData, encoding = "cipher+aes-256-cbc" };

                encoder.Decode(payload, _channelOptions);

                ((byte[])payload.data).Should().BeEquivalentTo(_binaryData);
                payload.encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithCipherPayloadBeforeOtherPayloads_DecryptsDataAndStriptsCipherEncoding()
            {
                var payload = new Message() { data = _encryptedBinaryData, encoding = "utf-8/cipher+aes-256-cbc" };

                encoder.Decode(payload, _channelOptions);

                ((byte[])payload.data).Should().BeEquivalentTo(_binaryData);
                payload.encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithOtherTypeOfPayload_LeavesDataAndEncodingIntact()
            {
                var payload = new Message() {data = "test", encoding = "utf-8"};

                encoder.Decode(payload, _channelOptions);

                payload.data.Should().Be("test");
                payload.encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithCipherEncodingThatDoesNotMatchTheCurrentCipher_LeavesMessageUnencrypted()
            {
                var initialEncoding = "utf-8/cipher+aes-128-cbc";
                var encryptedValue = "test";
                var payload = new Message() { data = encryptedValue, encoding = initialEncoding };

                encoder.Decode(payload, _channelOptions);

                payload.encoding.Should().Be(initialEncoding);
                payload.data.Should().Be(encryptedValue);

            }
        }

        public class DecodeWith256KeyLength : CipherEncoderTests
        {
            public DecodeWith256KeyLength() : base(keyLength: 256, encrypt: true)
            {

            }

            [Fact]
            public void WithCipherPayload_DercyptsDataAndStripsEncoding()
            {
                var payload = new Message() { data = _encryptedBinaryData, encoding = "cipher+aes-256-cbc" };

                encoder.Decode(payload, _channelOptions);

                ((byte[])payload.data).Should().BeEquivalentTo(_binaryData);
                payload.encoding.Should().BeEmpty();
            }
        }
    }
}