using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Tests.AblySandbox;
using Cysharp.Threading.Tasks;
using FluentAssertions;
using IO.Ably;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Assets.Tests.PlayMode
{
    [TestFixture]
    [Category("EditorPlayer")]
    public class StatsSandBoxSpecs
    {
        private AblySandboxFixture _sandboxFixture;

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            _sandboxFixture = new AblySandboxFixture();
        }

        [UnitySetUp]
        public IEnumerator Init()
        {
            AblySandbox = new Assets.Tests.AblySandbox.AblySandbox(_sandboxFixture);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AblySandbox.Dispose();
            yield return null;
        }

        public Assets.Tests.AblySandbox.AblySandbox AblySandbox { get; set; }


        private static readonly DateTimeOffset StartInterval =
            DateHelper.CreateDate(DateTimeOffset.UtcNow.Year - 1, 2, 3, 15, 5);

        private async Task<List<Stats>> GetStats(Protocol protocol)
        {
            var client = await AblySandbox.GetRestClient(protocol);
            var result = await client.StatsAsync(new StatsRequestParams
                {Start = StartInterval.AddMinutes(-2), End = StartInterval.AddMinutes(1)});

            return result.Items;
        }

        static Protocol[] _protocols = {Protocol.Json};

        [NUnit.Framework.Property("spec", "G3")]
        [UnityTest]
        public IEnumerator ShouldHaveCorrectStatsAsPerStatsSpec([ValueSource(nameof(_protocols))] Protocol protocol) =>
            UniTask.ToCoroutine(async () =>
            {
                await _sandboxFixture.SetupStats();

                async Task GetAndValidateStats()
                {
                    var allStats = await GetStats(protocol);
                    var stats = allStats.First();
                    stats.All.Messages.Count.Should().Be(40 + 70);
                    stats.All.Messages.Data.Should().Be(4000 + 7000);
                    stats.Inbound.Realtime.All.Count.Should().Be(70);
                    stats.Inbound.Realtime.All.Data.Should().Be(7000);
                    stats.Inbound.Realtime.Messages.Count.Should().Be(70);
                    stats.Inbound.Realtime.Messages.Data.Should().Be(7000);
                    stats.Outbound.Realtime.All.Count.Should().Be(40);
                    stats.Outbound.Realtime.All.Data.Should().Be(4000);
                    stats.Persisted.Presence.Count.Should().Be(20);
                    stats.Persisted.Presence.Data.Should().Be(2000);
                    stats.Connections.Tls.Peak.Should().Be(20);
                    stats.Connections.Tls.Opened.Should().Be(10);
                    stats.Channels.Peak.Should().Be(50);
                    stats.Channels.Opened.Should().Be(30);
                    stats.ApiRequests.Succeeded.Should().Be(50);
                    stats.ApiRequests.Failed.Should().Be(10);
                    stats.TokenRequests.Succeeded.Should().Be(60);
                    stats.TokenRequests.Failed.Should().Be(20);
                }

                await AblySandbox.AssertMultipleTimes(GetAndValidateStats, 5, TimeSpan.FromSeconds(5));
            });
    }
}