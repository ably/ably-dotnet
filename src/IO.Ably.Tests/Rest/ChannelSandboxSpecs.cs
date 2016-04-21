using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Rest
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class ChannelSandboxSpecs : SandboxSpecs
    {
        private JObject examples;


        public ChannelSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
            examples = JObject.Parse(ResourceHelper.GetResource("crypto-data-128.json"));
        }

        public ChannelOptions GetOptions()
        {
            var key = ((string) examples["key"]).FromBase64();
            var iv = ((string) examples["iv"]).FromBase64();
            var keyLength = (int) examples["keylength"];
            var cipherParams = new CipherParams("aes", key, CipherMode.CBC, keyLength, iv);
            return new ChannelOptions(cipherParams);
        }

        


        [Theory]
        [ProtocolData]
        public async Task CanPublishAMessageAndRetrieveIt(Protocol protocol)
        {
            var items = (JArray)examples["items"];

            AblyRest ably = await GetRestClient(protocol);
            IChannel channel = ably.Channels.Get("persisted:test", GetOptions());
            var count = 0;
            foreach (var item in items)
            {
                var encoded = item["encoded"];
                var encoding = (string)encoded["encoding"];
                var decodedData = DecodeData((string)encoded["data"], encoding);
                await channel.Publish((string)encoded["name"], decodedData);
                var message = (await channel.History()).First();
                if (message.data is byte[])
                    (message.data as byte[]).Should().BeEquivalentTo(decodedData as byte[], "Item number {0} data does not match decoded data", count);
                else if (encoding == "json")
                    JToken.DeepEquals((JToken)message.data, (JToken)decodedData).Should().BeTrue("Item number {0} data does not match decoded data", count);
                else
                    message.data.Should().Be(decodedData, "Item number {0} data does not match decoded data", count);
                count++;
            }
        }

        [Theory]
        [ProtocolData]
        public async Task Send20MessagesAndThenPaginateHistory(Protocol protocol)
        {
            //Arrange
            var client = await GetRestClient(protocol);
            IChannel channel = client.Channels.Get("persisted:historyTest:" + protocol);

            //Act
            for (int i = 0; i < 20; i++)
            {
                await channel.Publish("name" + i, "data" + i);
            }

            //Assert
            var history = await channel.History(new DataRequestQuery() { Limit = 10 });
            history.Should().HaveCount(10);
            history.HasNext.Should().BeTrue();
            history.First().name.Should().Be("name19");

            var secondPage = await channel.History(history.NextQuery);
            secondPage.Should().HaveCount(10);
            secondPage.First().name.Should().Be("name9");
        }

        private object DecodeData(string data, string encoding)
        {
            if (encoding == "json")
            {
                return JsonConvert.DeserializeObject(data);
            }
            if (encoding == "base64")
                return data.FromBase64();

            return data;
        }
    }
}
