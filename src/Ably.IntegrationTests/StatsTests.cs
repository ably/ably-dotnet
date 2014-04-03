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
                Encrypted = true
            };
            var ably = new Rest(options);
            return ably;
        }


        private static long timeOffset;
        private DateTime _start;
        private DateTime _end;

        [SetUp]
        public void Setup()
        {
            var ably = GetAbly();
            long timeFromService = ably.Time().ToUnixTimeInMilliseconds();
            timeOffset = timeFromService - DateTime.Now.ToUnixTimeInMilliseconds();

            /* first, wait for the start of a minute,
		     * to prevent earlier tests polluting our results */
            var now = (timeOffset + DateTime.Now.ToUnixTimeInMilliseconds()).FromUnixTimeInMilliseconds();

            _start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0);
            Thread.Sleep((int)(_start - now).TotalMilliseconds);

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

            var ably = GetAbly();

            /* wait for the stats to be persisted */

            var stats = ably.Stats(new StatsDataRequestQuery() { Start = _start, End = _end });
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);
        }


        [Test]
        public void HourLevelAppStats()
        {
            /* get the stats for this channel */
            var ably = GetAbly();
            var stats = ably.Stats(new StatsDataRequestQuery { Start = _start, End = _end, Unit = StatsUnit.Hour });
            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void DayLevelAppStats()
        {
            var ably = GetAbly();
            var stats = ably.Stats(new StatsDataRequestQuery() { Start = _start, End = _end, Unit = StatsUnit.Day });
            
            Assert.NotNull(stats);
            Assert.AreEqual(1,stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);
        }

        [Test]
        public void MonthLevelStats()
        {
            var ably = GetAbly();
            var stats = ably.Stats(new StatsDataRequestQuery() { Start = _start, End = _end, Unit = StatsUnit.Month });

            Assert.NotNull(stats);
            Assert.AreEqual(1, stats.Count());
            Assert.AreEqual(50, (int)stats.First().Inbound.All.All.Count);
        }

        ///**
        // * Publish events and check minute stats exist (backwards)
        // */
        //@Test
        //public void appstats_minute1() {
        //    /* first, wait for the start of a minute,
        //     * to prevent earlier tests polluting our results */
        //    long now = timeOffset + new Date().getTime();
        //    Date nextMinute = new Date(now);
        //    nextMinute.setSeconds(0);
        //    nextMinute.setMinutes(nextMinute.getMinutes() + 1);
        //    intervalStart = nextMinute.getTime();
        //    try {
        //        Thread.sleep(intervalStart - now);
        //    } catch(InterruptedException ie) {}

        //    /*publish some messages */
        //    Channel stats1 = ably.channels.get("appstats_1");
        //    for(int i = 0; i < 60; i++)
        //    try {
        //        stats1.publish("stats" + i,  new Integer(i));
        //    } catch(AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats1: Unexpected exception");
        //        return;
        //    }
        //    /* wait for the stats to be persisted */
        //    intervalEnd = timeOffset + System.currentTimeMillis();
        //    try {
        //        Thread.sleep(8000);
        //    } catch(InterruptedException ie) {}
        //    /* get the stats for this channel */
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "backwards"),
        //            new Param("start", String.valueOf(intervalStart)),
        //            new Param("end", String.valueOf(intervalEnd))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 record", stats.current.length, 1);
        //        assertEquals("Expected 60 messages", (int)stats.current[0].inbound.all.all.count, (int)60);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_minute1: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check hour-level stats exist (backwards)
        // */
        //@Test
        //public void appstats_hour1() {
        //    /* get the stats for this channel */
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "forwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("unit", "hour")
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertTrue("Expect 1 or two records", stats.current.length == 1 || stats.current.length == 2);
        //        if(stats.current.length == 1)
        //            assertEquals("Expected 110 messages", (int)stats.current[0].inbound.all.all.count, (int)110);
        //        else
        //            assertEquals("Expected 60 messages", (int)stats.current[1].inbound.all.all.count, (int)60);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_hour1: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check day-level stats exist (backwards)
        // */
        //@Test
        //public void appstats_day1() {
        //    /* get the stats for this channel */
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "forwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("unit", "day")
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertTrue("Expect 1 or two records", stats.current.length == 1 || stats.current.length == 2);
        //        if(stats.current.length == 1)
        //            assertEquals("Expected 110 messages", (int)stats.current[0].inbound.all.all.count, (int)110);
        //        else
        //            assertEquals("Expected 60 messages", (int)stats.current[1].inbound.all.all.count, (int)60);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_day1: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check month-level stats exist (backwards)
        // */
        //@Test
        //public void appstats_month1() {
        //    /* get the stats for this channel */
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "forwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("unit", "month")
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertTrue("Expect 1 or two records", stats.current.length == 1 || stats.current.length == 2);
        //        if(stats.current.length == 1)
        //            assertEquals("Expected 110 messages", (int)stats.current[0].inbound.all.all.count, (int)110);
        //        else
        //            assertEquals("Expected 60 messages", (int)stats.current[1].inbound.all.all.count, (int)60);

        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_month1: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Publish events and check limit query param (backwards)
        // */
        //@Test
        //public void appstats_limit0() {
        //    /* first, wait for the start of a minute,
        //     * to ensure we get records in distinct minutes */
        //    long now = timeOffset + new Date().getTime();
        //    Date nextMinute = new Date(now);
        //    nextMinute.setSeconds(0);
        //    nextMinute.setMinutes(nextMinute.getMinutes() + 1);
        //    intervalStart = nextMinute.getTime();
        //    try {
        //        Thread.sleep(intervalStart - now);
        //    } catch(InterruptedException ie) {}

        //    /*publish some messages */
        //    Channel stats2 = ably.channels.get("appstats_2");
        //    for(int i = 0; i < 70; i++)
        //    try {
        //        stats2.publish("stats" + i,  new Integer(i));
        //    } catch(AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats1: Unexpected exception");
        //        return;
        //    }
        //    /* wait for the stats to be persisted */
        //    intervalEnd = timeOffset + System.currentTimeMillis();
        //    try {
        //        Thread.sleep(8000);
        //    } catch(InterruptedException ie) {}
        //    /* get the stats for this channel */
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "backwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("limit", String.valueOf(1))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 70 messages", (int)stats.current[0].inbound.all.all.count, (int)70);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_limit0: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check limit query param (forwards)
        // */
        //@Test
        //public void appstats_limit1() {
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "forwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("limit", String.valueOf(1))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 50 messages", (int)stats.current[0].inbound.all.all.count, (int)50);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_limit1: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check query pagination (backwards)
        // */
        //@Test
        //public void appstats_pagination0() {
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "backwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("limit", String.valueOf(1))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 70 messages", (int)stats.current[0].inbound.all.all.count, (int)70);
        //        /* get next page */
        //        stats = stats.getNext();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 60 messages", (int)stats.current[0].inbound.all.all.count, (int)60);
        //        /* get next page */
        //        stats = stats.getNext();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 50 messages", (int)stats.current[0].inbound.all.all.count, (int)50);
        //        /* verify that there is no next page */
        //        assertNull("Expected null next page", stats.getNext());
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_pagination0: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check query pagination (forwards)
        // */
        //@Test
        //public void appstats_pagination1() {
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "forwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("limit", String.valueOf(1))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 50 messages", (int)stats.current[0].inbound.all.all.count, (int)50);
        //        /* get next page */
        //        stats = stats.getNext();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 60 messages", (int)stats.current[0].inbound.all.all.count, (int)60);
        //        /* get next page */
        //        stats = stats.getNext();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 70 messages", (int)stats.current[0].inbound.all.all.count, (int)70);
        //        /* verify that there is no next page */
        //        assertNull("Expected null next page", stats.getNext());
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_pagination1: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check query pagination rel="first" (backwards)
        // */
        //@Test
        //public void appstats_pagination2() {
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "backwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("limit", String.valueOf(1))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 70 messages", (int)stats.current[0].inbound.all.all.count, (int)70);
        //        /* get next page */
        //        stats = stats.getNext();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 60 messages", (int)stats.current[0].inbound.all.all.count, (int)60);
        //        /* get first page */
        //        stats = stats.getFirst();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 70 messages", (int)stats.current[0].inbound.all.all.count, (int)70);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_pagination2: Unexpected exception");
        //        return;
        //    }
        //}

        ///**
        // * Check query pagination rel="first" (forwards)
        // */
        //@Test
        //public void appstats_pagination3() {
        //    try {
        //        PaginatedResult<Stats[]> stats = ably.stats(new Param[] {
        //            new Param("direction", "forwards"),
        //            new Param("start", String.valueOf(testStart)),
        //            new Param("end", String.valueOf(intervalEnd)),
        //            new Param("limit", String.valueOf(1))
        //        });
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 50 messages", (int)stats.current[0].inbound.all.all.count, (int)50);
        //        /* get next page */
        //        stats = stats.getNext();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 60 messages", (int)stats.current[0].inbound.all.all.count, (int)60);
        //        /* get first page */
        //        stats = stats.getFirst();
        //        assertNotNull("Expected non-null stats", stats);
        //        assertEquals("Expected 1 records", stats.current.length, 1);
        //        assertEquals("Expected 50 messages", (int)stats.current[0].inbound.all.all.count, (int)50);
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("appstats_pagination3: Unexpected exception");
        //        return;
        //    }
        //}

    }
}
