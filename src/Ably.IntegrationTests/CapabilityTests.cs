using System.Linq;
using NUnit.Framework;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class CapabilityTests
    {
        private static Ably.Rest GetAbly()
        {
            var testData = TestsSetup.TestData;

            var options = new AblyOptions
            {
                Key = testData.keys.First().keyStr,
                Tls = false
            };
            var ably = new Rest(options);
            return ably;
        }
        /**
	 * Blanket intersection with specified key
	 */
        [Test]
        public void BlanketInsertions()
        {
            Key key = TestsSetup.TestData.keys[1];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenDetails = ably.Auth.RequestToken(null, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(key.capability, tokenDetails.Capability);
        }

        /**
         * Equal intersection with specified key
         */
        [Test]
        public void EqualIntersectionWithSpecificKey()
        {
            Key key = TestsSetup.TestData.keys[1];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            tokenParams.Capability = new Capability(key.capability);
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(key.capability, tokenDetails.Capability.ToString());
        }

        /**
         * Empty ops intersection
         */
        [Test]
        public void EmptyOpsIntersection()
        {
            Key key = TestsSetup.TestData.keys[1];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability capability = new Capability();
            capability.AddResource("testchannel").AllowSubscribe();
            tokenParams.Capability = capability;

            var ablyEx = Assert.Throws<AblyException>(
                delegate
                {
                    ably.Auth.RequestToken(tokenParams, authOptions);
                });
            Assert.AreEqual(40160, ablyEx.ErrorInfo.Code);
        }

        /**
         * Empty paths intersection
         */
        [Test]
        public void EmptyPathsIntersection()
        {
            Key key = TestsSetup.TestData.keys[1];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability capability = new Capability();
            capability.AddResource("testchannelx").AllowPublish();
            tokenParams.Capability = capability;
            var ablyEx = Assert.Throws<AblyException>(
                delegate
                {
                    ably.Auth.RequestToken(tokenParams, authOptions);
                });
            Assert.AreEqual(40160, ablyEx.ErrorInfo.Code, 40160);
        }

        /**
         * Non-empty ops intersection 
         */
        [Test]
        public void NotEmptyOpsIntersection()
        {
            Key key = TestsSetup.TestData.keys[4];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability capability = new Capability();
            capability.AddResource("channel2").AllowPresence().AllowSubscribe();
            tokenParams.Capability = capability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Capability expectedCapability = new Capability();
            expectedCapability.AddResource("channel2").AllowSubscribe();
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), expectedCapability.ToString());
        }

        /**
         * Non-empty paths intersection 
         */
        [Test]
        public void NonEmptyPathsIntersection()
        {
            Key key = TestsSetup.TestData.keys[4];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            var requestedCapability = new Capability();
            requestedCapability.AddResource("channel2").AllowPresence().AllowSubscribe();
            requestedCapability.AddResource("channelx").AllowPresence().AllowSubscribe();
            tokenParams.Capability = requestedCapability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Capability expectedCapability = new Capability();
            expectedCapability.AddResource("channel2").AllowSubscribe();
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), expectedCapability.ToString());
        }

        /**
         * Wildcard ops intersection 
         */
        [Test]
        public void WildcardOpsIntersection()
        {
            Key key = TestsSetup.TestData.keys[4];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability requestedCapability = new Capability();
            requestedCapability.AddResource("channel2").AllowAll();
            tokenParams.Capability = requestedCapability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Capability expectedCapability = new Capability();
            expectedCapability.AddResource("channel2").AllowPublish().AllowSubscribe();
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), expectedCapability);
        }

        [Test]
        public void authcapability7()
        {
            Key key = TestsSetup.TestData.keys[4];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability requestedCapability = new Capability();
            requestedCapability.AddResource("channel6").AllowPublish().AllowSubscribe();
            tokenParams.Capability = requestedCapability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Capability expectedCapability = new Capability();
            expectedCapability.AddResource("channel6").AllowPublish().AllowSubscribe();
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), expectedCapability.ToString());
        }

        /**
         * Wildcard resources intersection 
         */
        [Test]
        public void authcapability8()
        {
            Key key = TestsSetup.TestData.keys[2];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            var requestedCapability = new Capability();
            requestedCapability.AddResource("cansubscribe").AllowSubscribe();
            tokenParams.Capability = requestedCapability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), requestedCapability.ToString());
        }

        [Test]
        public void authcapability9()
        {
            Key key = TestsSetup.TestData.keys[2];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability requestedCapability = new Capability();
            requestedCapability.AddResource("canpublish:check").AllowPublish();
            tokenParams.Capability = requestedCapability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), requestedCapability.ToString());

        }

        [Test]
        public void authcapability10()
        {
            Key key = TestsSetup.TestData.keys[2];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var ably = GetAbly();
            var tokenParams = new TokenRequest();
            Capability requestedCapability = new Capability();
            requestedCapability.AddResource("cansubscribe:*").AllowSubscribe();
            tokenParams.Capability = requestedCapability;
            var tokenDetails = ably.Auth.RequestToken(tokenParams, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), requestedCapability.ToString());
        }
    }
}
