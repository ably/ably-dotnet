using System;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Encryption;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Tests
{
    public class AblySandboxFixture : IDisposable
    {
        public readonly static DateTimeOffset StartInterval = DateHelper.CreateDate(DateTimeOffset.UtcNow.Year - 1, 2, 3, 15, 5);

        private readonly AsyncLazy<TestEnvironmentSettings> _initSettings = new AsyncLazy<TestEnvironmentSettings>(async () => await Initialise());

        public async Task<TestEnvironmentSettings> GetSettings()
        {
            return await _initSettings;
        }

        private static async Task<TestEnvironmentSettings> Initialise()
        {
            var settings = new TestEnvironmentSettings()
            {
                Tls = true,
            };

            JObject testAppSpec = JObject.Parse(ResourceHelper.GetResource("test-app-setup.json"));

            var cipher = testAppSpec["cipher"];
            settings.CipherParams = new CipherParams(
                (string)cipher["algorithm"],
                ((string)cipher["key"]).FromBase64(),
                CipherMode.CBC,
                ((string)cipher["iv"]).FromBase64()
                );

            AblyHttpClient client = settings.GetHttpClient();
            AblyRequest request = new AblyRequest("/apps", HttpMethod.Post);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            request.RequestBody = testAppSpec["post_apps"].ToString().GetBytes();
            request.Protocol = Protocol.Json;

            var response = await client.Execute(request);

            var json = JObject.Parse(response.TextResponse);

            string appId = settings.AppId = (string)json["appId"];
            foreach (var key in json["keys"])
            {
                var testkey = new Key
                {
                    KeyName = appId + "." + (string) key["keyName"],
                    KeySecret = (string) key["keySecret"],
                    KeyStr = (string) key["keyStr"],
                    Capability = (string) key["capability"]
                };
                settings.Keys.Add(testkey);
            }

            await SetupSampleStats(settings);

            return settings;
        }

        public static async Task SetupSampleStats(TestEnvironmentSettings settings)
        {
            var lastInterval = StartInterval;
            var interval1 = lastInterval - TimeSpan.FromMinutes(120);
            var interval2 = lastInterval - TimeSpan.FromMinutes(60);
            var interval3 = lastInterval;
            var json = ResourceHelper.GetResource("StatsFixture.json");
            json = json.Replace("[[Interval1]]", interval1.ToString("yyyy-MM-dd:HH:mm"));
            json = json.Replace("[[Interval2]]", interval2.ToString("yyyy-MM-dd:HH:mm"));
            json = json.Replace("[[Interval3]]", interval3.ToString("yyyy-MM-dd:HH:mm"));

            AblyRest ablyRest = new AblyRest(settings.FirstValidKey);
            AblyHttpClient client = settings.GetHttpClient();
            var request = new AblyRequest("/stats", HttpMethod.Post);
            request.Protocol = Protocol.Json;
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            await ablyRest.AblyAuth.AddAuthHeader(request);
            request.RequestBody = json.GetBytes();

            await client.Execute(request);
        }   

        public void Dispose()
        {
            
        }
    }
}