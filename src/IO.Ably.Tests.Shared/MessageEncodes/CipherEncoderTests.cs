using System.Security.Cryptography;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.MessageEncoders;
using Xunit;

namespace IO.Ably.Tests.MessageEncodes
{
    public class CipherEncoderTests
    {
        private string _stringData;
        private byte[] _binaryData = { 2, 3, 4, 5, 6 };
        private byte[] _encryptedBinaryData;
        private byte[] _key;
        private CipherEncoder _encoder;
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

            _encoder = new CipherEncoder();
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
                var options = new ChannelOptions(new CipherParams(Crypto.DefaultAlgorithm, new byte[] { 1, 2, 3 }));
                var encoder = new CipherEncoder();
                var error = Assert.Throws<AblyException>(delegate
                {
                    encoder.Encode(new Message() { Data = "string" }, options.ToDecodingContext());
                });

                error.InnerException.Should().BeOfType<CryptographicException>();
            }

            [Fact]
            public void WithInvalidKey_Throws()
            {
                var options = new ChannelOptions(new CipherParams(Crypto.DefaultAlgorithm, new byte[] { 1, 2, 3 }));
                var encoder = new CipherEncoder();
                var error = Assert.Throws<AblyException>(delegate
                {
                    encoder.Encode(new Message() { Data = "string" }, options.ToDecodingContext());
                });

                error.InnerException.Should().BeOfType<CryptographicException>();
            }

            [Fact]
            public void WithInvalidAlgorithm_Throws()
            {
                var keyGen = new Rfc2898DeriveBytes("password", 8);
                var key = keyGen.GetBytes(Crypto.DefaultKeylength / 8);

                var options = new ChannelOptions(new CipherParams("mgg", key));
                var encoder = new CipherEncoder();
                var error = Assert.Throws<AblyException>(() =>
                {
                    encoder.Encode(new Message() { Data = "string" }, options.ToDecodingContext());
                });

                error.Message.Should().Contain("Currently only the AES encryption algorithm is supported");
            }
        }

        public class EncodeWith256CBCCipherParams : CipherEncoderTests
        {
            public EncodeWith256CBCCipherParams()
                : base(256, encrypt: true)
            {
            }

            [Fact]
            public void WithStringData_EncryptsDataAndSetsCorrectEncoding()
            {
                IPayload payload = new Message() { Data = _stringData };

                payload = _encoder.Encode(payload, _channelOptions.ToDecodingContext()).Value;

                var result =
                     _crypto.Decrypt(payload.Data as byte[]).GetText();

                result.Should().Be(_stringData);

                payload.Encoding.Should().Be("utf-8/cipher+aes-256-cbc");
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
                IPayload payload = new Message() { Data = _stringData };

                payload = _encoder.Encode(payload, _channelOptions.ToDecodingContext()).Value;

                string result = _crypto.Decrypt((byte[])payload.Data).GetText();
                result.Should().Be(_stringData);
                payload.Encoding.Should().Be("utf-8/cipher+aes-256-cbc");
            }

            [Fact]
            public void WithBinaryData_EncryptsTheDataAndAddsCorrectEncoding()
            {
                IPayload payload = new Message() { Data = _binaryData };

                payload = _encoder.Encode(payload, _channelOptions.ToDecodingContext()).Value;

                byte[] result = _crypto.Decrypt((byte[])payload.Data);
                result.Should().BeEquivalentTo(_binaryData);
                payload.Encoding.Should().Be("cipher+aes-256-cbc");
            }

            [Fact]
            public void WithJsonData_EncryptsTheDataAndAddsCorrectEncodings()
            {
                IPayload payload = new Message() { Data = _stringData, Encoding = "json" };

                payload = _encoder.Encode(payload, _channelOptions.ToDecodingContext()).Value;

                string result = _crypto.Decrypt((byte[])payload.Data).GetText();
                result.Should().BeEquivalentTo(_stringData);
                payload.Encoding.Should().Be("json/utf-8/cipher+aes-256-cbc");
            }

            [Fact]
            public void WithAlreadyEncryptedData_LeavesDataAndEncodingIntact()
            {
                IPayload payload = new Message() { Data = _encryptedData, Encoding = "utf-8/cipher+aes-256-cbc" };

                payload = _encoder.Encode(payload, _channelOptions.ToDecodingContext()).Value;

                payload.Data.Should().BeSameAs(_encryptedData);
                payload.Encoding.Should().Be("utf-8/cipher+aes-256-cbc");
            }
        }

        public class DecodeWithDefaultCipherParams : CipherEncoderTests
        {
            public DecodeWithDefaultCipherParams()
                : base(encrypt: true)
            {
            }

            [Fact]
            public void WithCipherPayload_DercyptsDataAndStripsEncoding()
            {
                IPayload payload = new Message() { Data = _encryptedBinaryData, Encoding = "cipher+aes-256-cbc" };

                payload = _encoder.Decode(payload, _channelOptions.ToDecodingContext()).Value;

                ((byte[])payload.Data).Should().BeEquivalentTo(_binaryData);
                payload.Encoding.Should().BeEmpty();
            }

            [Fact]
            public void WithCipherPayloadBeforeOtherPayloads_DecryptsDataAndStriptsCipherEncoding()
            {
                IPayload payload = new Message() { Data = _encryptedBinaryData, Encoding = "utf-8/cipher+aes-256-cbc" };

                payload = _encoder.Decode(payload, _channelOptions.ToDecodingContext()).Value;

                ((byte[])payload.Data).Should().BeEquivalentTo(_binaryData);
                payload.Encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithOtherTypeOfPayload_LeavesDataAndEncodingIntact()
            {
                IPayload payload = new Message() { Data = "test", Encoding = "utf-8" };

                payload = _encoder.Decode(payload, _channelOptions.ToDecodingContext()).Value;

                payload.Data.Should().Be("test");
                payload.Encoding.Should().Be("utf-8");
            }

            [Fact]
            public void WithCipherEncodingThatDoesNotMatchTheCurrentCipher_LeavesMessageUnencrypted()
            {
                 var initialEncoding = "utf-8/cipher+aes-128-cbc";
                 var encryptedValue = "test";
                 IPayload payload = new Message() { Data = encryptedValue, Encoding = initialEncoding };

                 var result = _encoder.Decode(payload, _channelOptions.ToDecodingContext());

                 result.IsFailure.Should().BeTrue();
                 payload.Encoding.Should().Be(initialEncoding);
                 payload.Data.Should().Be(encryptedValue);
            }
        }

        public class DecodeWith256KeyLength : CipherEncoderTests
        {
            public DecodeWith256KeyLength()
                : base(256, encrypt: true)
            {
            }

            [Fact]
            public void WithCipherPayload_DercyptsDataAndStripsEncoding()
            {
                IPayload payload = new Message() { Data = _encryptedBinaryData, Encoding = "cipher+aes-256-cbc" };

                payload = _encoder.Decode(payload, _channelOptions.ToDecodingContext()).Value;

                ((byte[])payload.Data).Should().BeEquivalentTo(_binaryData);
                payload.Encoding.Should().BeEmpty();
            }
        }
    }
}
