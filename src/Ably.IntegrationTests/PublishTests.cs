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
    public class PublishAndHistoryTests
    {
        [Test]
        public void CanPublishAMessageAndRetrieveIt()
        {
            var testData = TestsSetup.TestData;
            

            var options = new AblyOptions
            {
                Key = "TiHk3g:3lJG9Q:R8KadsOydTRCNMOp",
                Host = "rest.ably.io",
                Encrypted = testData.encrypted
            };
            var ably = new Rest(options);
            IChannel channel = ably.Channels.Get("test");
            channel.Publish("test", true);

            Thread.Sleep(10000);

            var messages = channel.History();

            Assert.AreEqual(1, messages.Count());
            Assert.True((bool)messages.First().Data);
        }
    }
}
