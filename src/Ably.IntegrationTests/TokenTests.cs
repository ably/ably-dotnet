using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class TokenTests
    {
        [Test]
        public void CanPublishWhenUsingTokenAuthentication()
        {
            var ably = new Rest(new AblyOptions { Encrypted = false, Key = TestsSetup.TestData.keys[0].keyStr });
            var token = ably.Auth.RequestToken(null, null);
            var tokenAbly = new Rest(new AblyOptions { Encrypted = false, AppId = TestsSetup.TestData.appId, AuthToken = token.Id });

            var testChannel = tokenAbly.Channels.Get("test");
           // testChannel.Publish("data", 1);

           // Thread.Sleep(10000);

            var data = testChannel.History(new HistoryDataRequestQuery { Direction = QueryDirection.Forwards });
            testChannel.History(new HistoryDataRequestQuery { By = HistoryBy.Hour, Direction = QueryDirection.Forwards });
            testChannel.History(new HistoryDataRequestQuery { By = HistoryBy.Bundle, Direction = QueryDirection.Forwards });
            Assert.AreEqual(1, data.First().Data);

        }
    }
}
