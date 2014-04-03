using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ably.IntegrationTests
{
    public static class TestHelpers
    {
        public static Ably.Rest GetAbly()
        {
            var testData = TestsSetup.TestData;


            var options = new AblyOptions
            {
                Key = testData.keys[0].keyStr,
                Encrypted = true
            };
            var ably = new Rest(options);
            return ably;
        }

    }


    [TestFixture]
    public class StatsTests
    {
        private static long timeOffset;
        private DateTime _start;
        private DateTime _end;

        [TestFixtureSetUp]
        public void Setup()
        {
            var ably = TestHelpers.GetAbly();
            long timeFromService = ably.Time().ToUnixTimeInMilliseconds();
            timeOffset = timeFromService - DateTime.Now.ToUnixTimeInMilliseconds();

            /* first, wait for the start of a minute,
		     * to prevent earlier tests polluting our results */
            var now = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();

            _start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0);
            Thread.Sleep((int) (_start - now).TotalMilliseconds);

            /*publish some messages */
            var stats0 = ably.Channels.Get("appstats_0");
            for (int i = 0; i < 50; i++)
                stats0.Publish("stats" + i, i);

            _end = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();
            //Wait for everything to be persisted
            Thread.Sleep(8000);

        }

        /**
         * Publish events and check minute-level stats exist (forwards)
         */

        [Test]
        public void MinuteLevelAppStats_minute0()
        {

            var ably = TestHelpers.GetAbly();

            /* wait for the stats to be persisted */

            var stats = ably.Stats(new StatsDataRequestQuery() {Start = _start, End = _end});
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int) stats.First().Inbound.All.All.Count);
        }


        [Test]
        public void HourLevelAppStats()
        {
            /* get the stats for this channel */
            var ably = TestHelpers.GetAbly();
            var stats = ably.Stats(new StatsDataRequestQuery {Start = _start, End = _end, Unit = StatsUnit.Hour});
            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int) stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void DayLevelAppStats()
        {
            var ably = TestHelpers.GetAbly();
            var stats = ably.Stats(new StatsDataRequestQuery() {Start = _start, End = _end, Unit = StatsUnit.Day});

            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int) stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void MonthLevelStats()
        {
            var ably = TestHelpers.GetAbly();
            var stats = ably.Stats(new StatsDataRequestQuery() {Start = _start, End = _end, Unit = StatsUnit.Month});

            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int) stats.First().Inbound.All.All.Count);
        }


    }

    [TestFixture]
    public class StatsTestsInReverseDirection
    {

        private static long timeOffset;
        private DateTime _start;
        private DateTime _end;

        [TestFixtureSetUp]
        public void Setup()
        {
            var ably = TestHelpers.GetAbly();
            long timeFromService = ably.Time().ToUnixTimeInMilliseconds();
            timeOffset = timeFromService - DateTime.Now.ToUnixTimeInMilliseconds();

            /* first, wait for the start of a minute,
		     * to prevent earlier tests polluting our results */
            var now = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();

            _start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0);
            Thread.Sleep((int) (_start - now).TotalMilliseconds);

            /*publish some messages */
            var stats0 = ably.Channels.Get("appstats_0");
            for (int i = 0; i < 60; i++)
                stats0.Publish("stats" + i, i);

            _end = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();
            //Wait for everything to be persisted
            Thread.Sleep(8000);

        }


        [Test]
        public void MinuteLevelAppStats_minute0()
        {

            var ably = TestHelpers.GetAbly();

            /* wait for the stats to be persisted */

            var stats =
                ably.Stats(new StatsDataRequestQuery()
                {
                    Start = _start,
                    End = _end,
                    Direction = QueryDirection.Backwards
                });
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(60, (int) stats.First().Inbound.All.All.Count);
        }


        [Test]
        public void HourLevelAppStats()
        {
            /* get the stats for this channel */
            var ably = TestHelpers.GetAbly();
            var stats =
                ably.Stats(new StatsDataRequestQuery
                {
                    Start = _start,
                    End = _end,
                    Unit = StatsUnit.Hour,
                    Direction = QueryDirection.Backwards
                });
            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(60, (int) stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void DayLevelAppStats()
        {
            var ably = TestHelpers.GetAbly();
            var stats =
                ably.Stats(new StatsDataRequestQuery()
                {
                    Start = _start,
                    End = _end,
                    Unit = StatsUnit.Day,
                    Direction = QueryDirection.Backwards
                });

            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(60, (int) stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void MonthLevelStats()
        {
            var ably = TestHelpers.GetAbly();
            var stats =
                ably.Stats(new StatsDataRequestQuery()
                {
                    Start = _start,
                    End = _end,
                    Unit = StatsUnit.Month,
                    Direction = QueryDirection.Backwards
                });

            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(60, (int) stats.First().Inbound.All.All.Count);
        }

    }

    [TestFixture]
    public class StatsTestsLimits
    {
        private static long timeOffset;
        private DateTime _start;
        private DateTime _end;

        [TestFixtureSetUp]
        public void Setup()
        {
            var ably = TestHelpers.GetAbly();
            long timeFromService = ably.Time().ToUnixTimeInMilliseconds();
            timeOffset = timeFromService - DateTime.Now.ToUnixTimeInMilliseconds();

            /* first, wait for the start of a minute,
		     * to prevent earlier tests polluting our results */
            var now = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();

            _start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0);
            Thread.Sleep((int)(_start - now).TotalMilliseconds);

            /*publish some messages */
            var stats0 = ably.Channels.Get("appstats_0");
            for (int i = 0; i < 60; i++)
                stats0.Publish("stats" + i, i);

            _end = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();
            //Wait for everything to be persisted
            Thread.Sleep(8000);

        }


        //Bad test because it depends on other tests running first
        [Test]
        public void MinuteStatsForTheLastHour_DirectionForward()
        {

            var ably = TestHelpers.GetAbly();

            /* wait for the stats to be persisted */

            var stats =
                ably.Stats(new StatsDataRequestQuery()
                {
                    Start = _start.AddDays(-1),
                    End = _end,
                    Direction = QueryDirection.Forwards,
                    Limit = 1
                });


            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void GetAllMinuteStatsForTheLastHour_ToTestPaginationGoingForward()
        {
            var ably = TestHelpers.GetAbly();

            /* wait for the stats to be persisted */

            var stats =
                ably.Stats(new StatsDataRequestQuery()
                {
                    Start = _start.AddDays(-1),
                    End = _end,
                    Direction = QueryDirection.Forwards,
                    Limit = 1
                });


            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);

            stats = ably.Stats(stats.NextQuery);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(60, (int)stats.First().Inbound.All.All.Count);

            stats = ably.Stats(stats.NextQuery);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(70, (int)stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void GetAllMinuteStatsForTheLastHour_ToTestPaginationGoingBackwards()
        {
            var ably = TestHelpers.GetAbly();

            /* wait for the stats to be persisted */

            var stats =
                ably.Stats(new StatsDataRequestQuery()
                {
                    Start = _start.AddDays(-1),
                    End = _end,
                    Direction = QueryDirection.Backwards,
                    Limit = 1
                });




            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(70, (int)stats.First().Inbound.All.All.Count);

            stats = ably.Stats(stats.NextQuery);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(60, (int)stats.First().Inbound.All.All.Count);

            stats = ably.Stats(stats.NextQuery);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);
        }

    }
}
