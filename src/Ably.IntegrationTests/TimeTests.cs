using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class TimeTests
    {
        /**
	     * Verify accuracy of time (to within 2 seconds of actual time)
	     */
	[Test]
	public void TimeFromAblyServerShouldBeCloseToCurrentTime() {
			TestVars testData = TestsSetup.TestData;
            AblyOptions opts = new AblyOptions();
            opts.Key = "AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob";
			opts.Encrypted = false;
			Rest ably = new Rest(opts);
			var serverTime = ably.Time();
			var localTime = DateTimeOffset.UtcNow;
            var difference = localTime - serverTime;
            Assert.Less(difference.TotalMinutes, 1);
		
	}

	/**
	 * Verify time can be obtained without any valid key or token
	 */
	//@Test
	//public void time1() {
	//	try {
	//		TestVars testVars = RestSetup.getTestVars();
	//		Options opts = new Options();
	//		opts.appId = testVars.appId;
	//		opts.restHost = testVars.restHost;
	//		opts.restPort = testVars.restPort;
	//		opts.encrypted = testVars.encrypted;
	//		AblyRest ablyNoAuth = new AblyRest(opts);
	//		ablyNoAuth.time();
	//	} catch (AblyException e) {
	//		e.printStackTrace();
	//		fail("time1: Unexpected exception getting time");
	//	}
	//}

	///**
	// * Verify time fails without valid host
	// */
	//@Test
	//public void time2() {
	//	try {
	//		TestVars testVars = RestSetup.getTestVars();
	//		Options opts = new Options();
	//		opts.appId = testVars.appId;
	//		opts.restHost = "this.host.does.not.exist";
	//		opts.restPort = testVars.restPort;
	//		AblyRest ably = new AblyRest(opts);
	//		ably.time();
	//		fail("time2: Unexpected success getting time");
	//	} catch (AblyException e) {
	//		assertEquals("time2: Unexpected error code", e.statusCode, 404);
	//	}
	//}
    }
}
