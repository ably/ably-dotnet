using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Ably.AcceptanceTests
{
    [SetUpFixture]
    public class TestsSetup
    {
        public static TestVars TestData;

        private static TestVars GetTestData() 
        {
            return new TestVars() { tls = true, restHost = "sandbox-rest.ably.io", keys = new List<Key>() };
        }

        public static Ably.AblyOptions GetDefaultOptions()
        {
            return new AblyOptions
            {
                Key = TestData.keys[0].keyStr,
            };
        }
        [SetUp]
        public void RunBeforeAllTests()
        {
            TestData = GetTestData();
            Config.DefaultHost = TestData.restHost;
            AblyHttpClient client = new AblyHttpClient(TestData.restHost, null, TestData.tls);
            AblyRequest request = new AblyRequest("/apps", HttpMethod.Post);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            request.PostData = JToken.Parse(File.ReadAllText("testAppSpec.json"));
            var response = client.Execute(request);
            var json = JObject.Parse(response.TextResponse);

            string appId = TestData.appId = (string)json["id"];
            foreach (var key in json["keys"])
            {
                var testkey = new Key();
                testkey.keyId = appId + "." + (string)key["id"];
                testkey.keyValue = (string)key["value"];
                testkey.keyStr = testkey.keyId + ":" + testkey.keyValue;
                testkey.capability = (string)key["capability"];
                TestData.keys.Add(testkey);
            }
        }

        [TearDown]
        public void RunAfterAllTests()
        {
            var options = new AblyOptions { Key = TestData.keys[0].keyStr };

            var rest = new Rest(options);

            AblyRequest request = new AblyRequest("/apps/" + TestData.appId, HttpMethod.Delete);
            request.Headers.Add("Accept", "application/json");
            rest.ExecuteRequest(request);
        }
    }
}
