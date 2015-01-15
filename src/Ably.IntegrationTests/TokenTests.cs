using NUnit.Framework;
using System;
using System.Linq;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class TokenTests
    {
        [Test]
        public void CanPublishWhenUsingTokenAuthentication()
        {
            var ably = new Rest(new AblyOptions { Tls = false, Key = TestsSetup.TestData.keys[0].keyStr });
            var token = ably.Auth.RequestToken(null, null);
            var tokenAbly = new Rest(new AblyOptions { Tls = false, AppId = TestsSetup.TestData.appId, AuthToken = token.Id });

            var testChannel = tokenAbly.Channels.Get("persisted:test");


            var data = testChannel.History(new HistoryDataRequestQuery { Direction = QueryDirection.Forwards });
            testChannel.History(new HistoryDataRequestQuery { By = HistoryBy.Hour, Direction = QueryDirection.Forwards });
            testChannel.History(new HistoryDataRequestQuery { By = HistoryBy.Bundle, Direction = QueryDirection.Forwards });
            Assert.AreEqual(1, data.First().Data);

        }

        private string permitAll;
        private Rest _ably;

        [SetUp]
        public void Setup()
        {
            Capability capability = new Capability();
            capability.AddResource("*").AllowAll();
            permitAll = capability.ToString();
            TestVars testVars = TestsSetup.TestData;
            var opts = testVars.CreateOptions(testVars.keys[0].keyStr);
            _ably = new Rest(opts);
        }

        /**
         * Base requestToken case with null params
         */
        [Test]
        public void RequestTokenWithEmptyParameters()
        {
            DateTime requestTime = DateTime.Now;
            var tokenDetails = _ably.Auth.RequestToken(null, null);
            Assert.NotNull(tokenDetails.Id);
            Assert.True((tokenDetails.IssuedAt >= (requestTime.AddSeconds(-1))) && (tokenDetails.IssuedAt <= (requestTime.AddSeconds(1))));
            Assert.AreEqual(tokenDetails.ExpiresAt, tokenDetails.IssuedAt.AddSeconds(60 * 60));
            Assert.AreEqual(tokenDetails.Capability.ToString(), permitAll);
        }

        /**
         * Base requestToken case with non-null but empty params
         */
        [Test]
        public void RequestWithEmptyTokenRequest_ReturnsCorrectToken()
        {
            DateTime requestTime = DateTime.Now;

            var tokenDetails = _ably.Auth.RequestToken(new TokenRequest(), null);
            Assert.NotNull(tokenDetails.Id);
            Assert.True((tokenDetails.IssuedAt >= (requestTime.AddSeconds(-1))) && (tokenDetails.IssuedAt <= (requestTime.AddSeconds(1))));
            Assert.AreEqual(tokenDetails.ExpiresAt, tokenDetails.IssuedAt.AddSeconds(60 * 60));
            Assert.AreEqual(tokenDetails.Capability.ToString(), permitAll);
        }

        /**
         * requestToken with explicit timestamp
         */
        [Test]
        public void RequestToken_WithSpecificTime()
        {
            var requestTime = Config.Now().AddMinutes(1);
            TokenRequest request = new TokenRequest();
            request.Timestamp = requestTime;
            var tokenDetails = _ably.Auth.RequestToken(request, null);
            Assert.NotNull(tokenDetails.Id);
            Assert.True((tokenDetails.IssuedAt >= (requestTime.AddSeconds(-1))) && (tokenDetails.IssuedAt <= (requestTime.AddSeconds(1))));
            Assert.AreEqual(tokenDetails.ExpiresAt, tokenDetails.IssuedAt.AddSeconds(60 * 60));
            Assert.AreEqual(tokenDetails.Capability.ToString(), permitAll);
        }

        /**
         * requestToken with explicit, invalid timestamp
         */
        [Test]
        public void RequestToken_WithInvalidTimeStamp_ThrowsInvalidError()
        {
            var requestTime = Config.Now().AddMinutes(-30);
            var request = new TokenRequest() { Timestamp = requestTime };

            var exception = Assert.Throws<AblyException>(delegate
            {
                _ably.Auth.RequestToken(request, null);
            });

            Assert.Equals(40101, exception.ErrorInfo.Code);
        }

        /**
	 * requestToken with system timestamp
	 */
        [Test]
        public void RequestToken_WhenSystemQueriesTimeSetToTrue_ReturnScorrectToken()
        {
            DateTime requestTime = DateTime.Now;
            var authOptions = new AuthOptions();
            authOptions.QueryTime = true;
            var tokenDetails = _ably.Auth.RequestToken(null, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.True((tokenDetails.IssuedAt >= (requestTime.AddSeconds(-1))) && (tokenDetails.IssuedAt <= (requestTime.AddSeconds(1))));
            Assert.AreEqual(tokenDetails.ExpiresAt, tokenDetails.IssuedAt.AddSeconds(60 * 60));
            Assert.AreEqual(tokenDetails.Capability.ToString(), permitAll);
        }

        /**
         * Base requestToken case with non-null but empty params
         */
        [Test]
        public void RequestToken_WithEmptyTokenParamsExceptClientId_ReturnsCorrectToken()
        {
            DateTime requestTime = DateTime.Now;

            string clientId = "test client";
            var tokenDetails = _ably.Auth.RequestToken(new TokenRequest() { ClientId = clientId }, null);
            Assert.NotNull(tokenDetails.Id);
            Assert.True((tokenDetails.IssuedAt >= (requestTime.AddSeconds(-1))) && (tokenDetails.IssuedAt <= (requestTime.AddSeconds(1))));
            Assert.AreEqual(tokenDetails.ExpiresAt, tokenDetails.IssuedAt.AddSeconds(60 * 60));
            Assert.AreEqual(tokenDetails.Capability.ToString(), permitAll);
            Assert.AreEqual(tokenDetails.ClientId, clientId);
        }

        /**
         * Token generation with capability that subsets key capability
         */
        [Test]
        public void RequestToken_WithCapability_ReturnsTokenWithCapability()
        {
            var tokenParams = new TokenRequest();
            var capability = new Capability();
            capability.AddResource("onlythischannel").AllowSubscribe();
            tokenParams.Capability = capability;
            var tokenDetails = _ably.Auth.RequestToken(tokenParams, null);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), capability.ToString());
        }

        /**
         * Token generation with specified key
         */
        [Test]
        public void RequestToken_WithAKeyThatHaveSpecificCapability_ReturnsTokenWithTheSameCapability()
        {
            Key key = TestsSetup.TestData.keys[1];
            AuthOptions authOptions = new AuthOptions();
            authOptions.KeyId = key.keyId;
            authOptions.KeyValue = key.keyValue;
            var tokenDetails = _ably.Auth.RequestToken(null, authOptions);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.Capability.ToString(), key.capability);
        }

        /**
         * Token generation with specified ttl
         */
        [Test]
        public void RequestToken_WithSpecificTimeToLive_ReturnsTokenWithRequestedTimeToLive()
        {
            var tokenParams = new TokenRequest();
            tokenParams.Ttl = TimeSpan.FromSeconds(100);
            var tokenDetails = _ably.Auth.RequestToken(tokenParams, null);
            Assert.NotNull(tokenDetails.Id);
            Assert.AreEqual(tokenDetails.ExpiresAt, tokenDetails.IssuedAt.Add(tokenParams.Ttl.Value));
        }

        /**
         * Token generation with excessive ttl
         */
        [Test]
        public void RequestToken_WithExcessiveTtl_ThrowsAnError()
        {
            var tokenParams = new TokenRequest();
            tokenParams.Ttl = TimeSpan.FromSeconds(365 * 24 * 60 * 60);
            var exception = Assert.Throws<AblyException>(delegate
            {
                _ably.Auth.RequestToken(tokenParams, null);
            });
            Assert.AreEqual(40003, exception.ErrorInfo.Code);
        }

        /**
         * Token generation with invalid ttl
         */
        [Test]
        public void RequestToken_WithInvalidTimeToLive_ThrowsAnError()
        {
            var tokenParams = new TokenRequest();
            tokenParams.Ttl = TimeSpan.FromSeconds(-1);
            var exception = Assert.Throws<AblyException>(delegate
            {
                _ably.Auth.RequestToken(tokenParams, null);
            });
            Assert.AreEqual(40003, exception.ErrorInfo.Code);
        }
    }

}

