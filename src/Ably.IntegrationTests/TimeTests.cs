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
        
        [Test]
        public void TimeFromAblyServerShouldBeCloseToCurrentTime()
        {
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

        [Test]
        public void CanObtainTimeWithoutValidKeyOrToken()
        {
            var options = new AblyOptions();
            options.AppId = "AHSz6w";
            options.Encrypted = false;
            Rest ably = new Rest(options);
            var serverTime = ably.Time();
            Assert.Greater(serverTime, DateTime.MinValue);
        }

        [Test]
        public void TimeFailsWithInvalidHost()
        {
            var options = new AblyOptions();
            options.AppId = "AHSz6w";
            options.Host = "this.host.does.not.exist";
            options.Encrypted = false;
            Rest ably = new Rest(options);

            Assert.Throws<AblyException>(delegate
            {
                ably.Time();
            });
        }

    }
}
