using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ably.IntegrationTests
{
    [SetUpFixture]
    public class TestsSetup
    {
        public static TestVars TestData;

        private static TestVars GetTestData()
        {
            return new TestVars() { encrypted = false, restHost = "rest.ably.io", keys = new List<Key>() };
        }
        [SetUp]
        public void RunBeforeAllTests()
        {
            Config.DefaultHost = "rest.ably.io";
               TestData = GetTestData();
         //   AblyHttpClient client = new AblyHttpClient(TestData.restHost, null, false);
         //   AblyRequest request = new AblyRequest("/apps", HttpMethod.Post);
         //   request.Headers.Add("Accept", "application/json");
         //   request.Headers.Add("Content-Type", "application/json");
         //   request.PostData = File.ReadAllText("testAppSpec.json");
         //   var response = client.Execute(request);
         //   var json = JObject.Parse(response.JsonResult);
         //   
               TestData.appId = "BO8VEg";
               var testKey = new Key() { keyStr = "BO8VEg:VMdFSQ:YXMuQMOOw69Fw681" };
            TestData.keys.Add(testKey);
         //   foreach (var key in json["keys"])
	        //{
         //       var testkey = new Key();
         //       testkey.keyId = (string)key["id"];
         //       testkey.keyValue = (string)key["value"];
         //       testkey.keyStr = String.Format("{0}:{1}:{2}", TestData.appId, testkey.keyId, testkey.keyValue);
         //       testkey.capability = (string)key["capability"];
         //       TestData.keys.Add(testkey);
	        //}
        }

        [TearDown]
        public void RunAfterAllTests()
        {
            //var options = new AblyOptions { Key = TestData.keys[0].keyStr, Encrypted = TestData.encrypted };
            //     
            //var rest = new Rest(options);

            //AblyRequest request = new AblyRequest("/apps/" + TestData.appId, HttpMethod.Delete);
            //request.Headers.Add("Accept", "application/json");
            //rest.ExecuteRequest(request);
        }
    }
}
