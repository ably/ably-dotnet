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
    public class StatsTests
    {
        private static Ably.Rest GetAbly()
        {
            var testData = TestsSetup.TestData;


            var options = new AblyOptions
            {
                Key = testData.keys[0].keyStr,
                Host = "rest.ably.io",
                Encrypted = false
            };
            var ably = new Rest(options);
            return ably;
        }

        [Test]
        public void CanRetrieveStats()
        {
            Ably.Rest ably = GetAbly();
            IChannel channel = ably.Channels.Get("test");
            var time = ably.Time();
            for (int i = 0; i < 20; i++)
            { 
                channel.Publish("test", true);
            }
            var end = ably.Time();

            Thread.Sleep(120000);

            var stats = ably.Stats(new DataRequestQuery { Start = time, Direction = QueryDirection.Forwards });

            Assert.NotNull(stats);
            Assert.AreEqual(20, stats.First().All.All.Count);

            var backwardsStats = ably.Stats(new DataRequestQuery { Start = end, End = time, Direction = QueryDirection.Backwards });

            Assert.AreEqual(20, backwardsStats.First().All.All.Count);
            
        }


    }
}
