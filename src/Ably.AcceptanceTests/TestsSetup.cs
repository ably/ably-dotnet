﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IO.Ably.AcceptanceTests
{
    [SetUpFixture]
    public class TestsSetup
    {
        public static TestVars TestData;

        private static TestVars GetTestData()
        {
            return new TestVars { tls = true, keys = new List<Key>(), Environment = AblyEnvironment.Sandbox};
        }

        public static T GetDefaultOptions<T>()
            where T : ClientOptions, new()
        {
            return new T
            {
                Key = TestData.keys[0].keyStr,
                Environment = TestData.Environment,
                Tls = TestData.tls
            };
        }

        public static ClientOptions GetDefaultOptions()
        {
            return GetDefaultOptions<ClientOptions>();
        }

        [OneTimeSetUp]
        public async Task RunBeforeAllTests()
        {
            TestData = GetTestData();
            
            TestData.TestAppSpec = JObject.Parse(ResourceHelper.GetResource("testAppSpec.json"));
            var httpOptions = new AblyHttpOptions() {Host = TestData.RestHost, IsSecure = TestData.tls };
            AblyHttpClient client = new AblyHttpClient(httpOptions);
            AblyRequest request = new AblyRequest("/apps", HttpMethod.Post);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            request.RequestBody = TestData.TestAppSpec.ToString().GetBytes();
            request.Protocol = Protocol.Json;

            AblyResponse response;
            response = client.Execute(request).Result;
            
            var json = JObject.Parse(response.TextResponse);

            string appId = TestData.appId = (string)json["appId"];
            foreach (var key in json["keys"])
            {
                var testkey = new Key();
                testkey.keyName = appId + "." + (string)key["keyName"];
                testkey.keySecret = (string)key["keySecret"];
                testkey.keyStr = (string)key["keyStr"];
                testkey.capability = (string)key["capability"];
                TestData.keys.Add(testkey);
            }

            await SetupSampleStats();
        }

        public async Task SetupSampleStats()
        {
            var lastInterval = StatsAcceptanceTests.StartInterval;
            var interval1 = lastInterval - TimeSpan.FromMinutes(120);
            var interval2 = lastInterval - TimeSpan.FromMinutes(60);
            var interval3 = lastInterval;
            var json = ResourceHelper.GetResource("StatsFixture.json");
            json = json.Replace("[[Interval1]]", interval1.ToString("yyyy-MM-dd:HH:mm"));
            json = json.Replace("[[Interval2]]", interval2.ToString("yyyy-MM-dd:HH:mm"));
            json = json.Replace("[[Interval3]]", interval3.ToString("yyyy-MM-dd:HH:mm"));

            AblyRest ablyRest = new AblyRest(TestData.keys.First().keyStr);
            var httpOptions = new AblyHttpOptions() { Host = TestData.RestHost, IsSecure = TestData.tls };
            AblyHttpClient client = new AblyHttpClient(httpOptions);
            AblyRequest request = new AblyRequest("/stats", HttpMethod.Post);
            request.Protocol = Protocol.Json;
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            await ablyRest.AblyAuth.AddAuthHeader(request);
            request.RequestBody = json.GetBytes();

            var response = client.Execute(request).Result;
        }

        [OneTimeTearDown]
        public void RunAfterAllTests()
        {
        }
    }
}
