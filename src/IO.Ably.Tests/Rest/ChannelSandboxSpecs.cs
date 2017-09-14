using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.Rest;
using IO.Ably;
using IO.Ably.Tests.Infrastructure;
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
        private JObject examples256;

        public ChannelSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
            examples = JObject.Parse(ResourceHelper.GetResource("crypto-data-128.json"));
            examples256 = JObject.Parse(ResourceHelper.GetResource("crypto-data-256.json"));
        }

        public ChannelOptions GetOptions(JObject data)
        {
            var key = ((string) data["key"]);
            var iv = ((string) data["iv"]);
            var cipherParams = new CipherParams("aes", key, CipherMode.CBC, iv);
            return new ChannelOptions(cipherParams);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1c")]
        [Trait("spec", "RSL1d")]
        public async Task SendingAVeryLargeMessage_ShouldThrowErrorToIndicateSendingFailed(Protocol protocol)
        {
            var message = new Message();
            message.Name = "large";
            message.Data = new string('a', 50 * 1024 * 8); // 100KB
            var client = await GetRestClient(protocol);
            var ex = await Assert.ThrowsAsync<AblyException>(()
                => client.Channels.Get("large").PublishAsync(message));
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1f1")]
        public async Task WithBasicAuthWhenMessageHasClientId_ShouldRetrieveMessageWithSameClientId(Protocol protocol)
        {
            var message = new Message("test", "test") {ClientId = "123"};
            var client = await GetRestClient(protocol);
            var channel = client.Channels.Get("persisted:test");
            await channel.PublishAsync(message);

            var result = await channel.HistoryAsync();
            result.Items.First().ClientId.Should().Be("123");
        }

        //RSL1g
        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1g1b")]
        public async Task WithImplicitClientIdComingFromOptions_ReturnsMessageWithCorrectClientId(Protocol protocol)
        {
            var message = new Message("test", "test") { ClientId = null};
            var client = await GetRestClient(protocol, opts => opts.ClientId = "999");
            var channel = client.Channels.Get("persisted:test".AddRandomSuffix());
            await channel.PublishAsync(message);

            var result = await channel.HistoryAsync();
            result.Items.First().ClientId.Should().Be("999");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1g2")]
        public async Task WithExplicitClientIdMatchingClientIdInOptions_ReturnsMessageWithCorrectClientId(Protocol protocol)
        {
            var message = new Message("test", "test") { ClientId = "999" };
            var client = await GetRestClient(protocol, opts => opts.ClientId = "999");
            var channel = client.Channels.Get("persisted:test".AddRandomSuffix());
            await channel.PublishAsync(message);

            var result = await channel.HistoryAsync();
            result.Items.First().ClientId.Should().Be("999");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1g3")]
        [Trait("spec", "RSL1g4")]
        public async Task WithExplicitClientIdNotMatchingClientIdInOptions_ReturnsMessageWithCorrectClientId(Protocol protocol)
        {
            var message = new Message("test", "test") { ClientId = "999" };
            var client = await GetRestClient(protocol, opts => opts.ClientId = "111");
            var channel = client.Channels.Get("test");
            var ex = await Assert.ThrowsAsync<AblyException>(() => channel.PublishAsync(message));

            //Can publish further messages in the same channel
            await channel.PublishAsync("test", "test");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL4c4")]
        [Trait("spec", "RSL4d4")]
        [Trait("spec", "RSL5a")]
        [Trait("spec", "RSL5c")]
        [Trait("spec", "RSL6a")]
        //Uses the to publish the examples inside crypto-data-128.json to publish and then retrieve the messages
        public async Task CanPublishAMessageAndRetrieveIt128(Protocol protocol)
        {
            var items = (JArray)examples["items"];

            AblyRest ably = await GetRestClient(protocol);
            IRestChannel channel = ably.Channels.Get("persisted:test".AddRandomSuffix(), GetOptions(examples));
            var count = 0;
            foreach (var item in items)
            {
                var encoded = item["encoded"];
                var encoding = (string)encoded["encoding"];
                var decodedData = DecodeData((string)encoded["data"], encoding);
                await channel.PublishAsync((string)encoded["name"], decodedData);
                var message = (await channel.HistoryAsync()).Items.First();
                if (message.Data is byte[])
                    (message.Data as byte[]).Should().BeEquivalentTo(decodedData as byte[], "Item number {0} data does not match decoded data", count);
                else if (encoding == "json")
                    JToken.DeepEquals((JToken)message.Data, (JToken)decodedData).Should().BeTrue("Item number {0} data does not match decoded data", count);
                else
                    message.Data.Should().Be(decodedData, "Item number {0} data does not match decoded data", count);
                count++;
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL5b")]
        [Trait("spec", "RSL5c")]
        //Uses the to publish the examples inside crypto-data-256.json to publish and then retrieve the messages
        public async Task CanPublishAMessageAndRetrieveIt256(Protocol protocol)
        {
            var items = (JArray)examples256["items"];

            AblyRest ably = await GetRestClient(protocol);
            IRestChannel channel = ably.Channels.Get("persisted:test".AddRandomSuffix(), GetOptions(examples256));
            var count = 0;
            foreach (var item in items)
            {
                var encoded = item["encoded"];
                var encoding = (string)encoded["encoding"];
                var decodedData = DecodeData((string)encoded["data"], encoding);
                await channel.PublishAsync((string)encoded["name"], decodedData);
                var message = (await channel.HistoryAsync()).Items.First();
                if (message.Data is byte[])
                    (message.Data as byte[]).Should().BeEquivalentTo(decodedData as byte[], "Item number {0} data does not match decoded data", count);
                else if (encoding == "json")
                    JToken.DeepEquals((JToken)message.Data, (JToken)decodedData).Should().BeTrue("Item number {0} data does not match decoded data", count);
                else
                    message.Data.Should().Be(decodedData, "Item number {0} data does not match decoded data", count);
                count++;
            }
        }

        [Theory]
        [ProtocolData]
        public async Task Send20MessagesAndThenPaginateHistory(Protocol protocol)
        {
            //Arrange
            var client = await GetRestClient(protocol);
            IRestChannel channel = client.Channels.Get("persisted:historyTest:" + protocol);

            //Act
            for (int i = 0; i < 20; i++)
            {
                await channel.PublishAsync("name" + i, "data" + i);
            }

            //Assert
            var history = await channel.HistoryAsync(new HistoryRequestParams() { Limit = 10 });
            history.Items.Should().HaveCount(10);
            history.HasNext.Should().BeTrue();
            history.Items.First().Name.Should().Be("name19");

            var secondPage = await history.NextAsync();
            secondPage.Items.Should().HaveCount(10);
            secondPage.Items.First().Name.Should().Be("name9");
        }

        [Theory]
        [ProtocolData]
        public async Task Send20MessagesAndThenPaginateHistorySync(Protocol protocol)
        {
            //Arrange
            var client = await GetRestClient(protocol);
            IRestChannel channel = client.Channels.Get("persisted:historyTest:" + protocol);

            //Act
            for (int i = 0; i < 20; i++)
            {
                channel.Publish("name" + i, "data" + i);
            }

            //Assert
            var history = channel.History(new HistoryRequestParams() { Limit = 10 });
            history.Items.Should().HaveCount(10);
            history.HasNext.Should().BeTrue();
            history.Items.First().Name.Should().Be("name19");

            var secondPage = history.Next();
            secondPage.Items.Should().HaveCount(10);
            secondPage.Items.First().Name.Should().Be("name9");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL4a")]
        public async Task WithUnsupportedPayloadTypes_ShouldRaiseException(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var channel = client.Channels.Get("persisted:test_" + protocol);

            var ex = await Assert.ThrowsAsync<AblyException>(() => channel.PublishAsync("int", 1));
        }

        class TestLoggerSink : ILoggerSink
        {
            public LogLevel LastLoggedLevel { get; set; }
            public string LastMessage { get; set; }
            public void LogEvent(LogLevel level, string message)
            {
                LastLoggedLevel = level;
                LastMessage = message;
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL6b")]
        public async Task WithEncryptionCipherMismatch_ShouldLeaveMessageEncryptedAndLogError(Protocol protocol)
        {
            var loggerSink = new TestLoggerSink();
            ILogger logger = new IO.Ably.Logger.InternalLogger(LogLevel.Error, loggerSink);

            logger.LogLevel.ShouldBeEquivalentTo(LogLevel.Error);
            logger.IsDebug.ShouldBeEquivalentTo(false);
            
            var client = await GetRestClient(protocol, options =>
            {
                options.Logger = logger; // pass the logger into the client
            });

            var opts = GetOptions(examples);
            opts.Logger = logger;
            var channel1 = client.Channels.Get("persisted:encryption", opts );

            var payload = "test payload";
            await channel1.PublishAsync("test", payload);

            var channel2 = client.Channels.Get("persisted:encryption", new ChannelOptions(logger, true));
            var message = (await channel2.HistoryAsync()).Items.First();

            loggerSink.LastLoggedLevel.Should().Be(LogLevel.Error);
            message.Encoding.Should().Be("utf-8/cipher+aes-128-cbc");
            
        }

        [Theory]
        [InteropabilityMessagePayloadData]
        [Trait("spec", "RSL6a1")]
        public async Task WithTestMessagePayloadsWhenDecoding_ShouldDecodeMessagesAsPerSpec(Protocol protocol,
            JObject messageData)
        {
            Logger.LogLevel = LogLevel.Debug;
            
            var channelName = "channel-name-" + new Random().Next(int.MaxValue);

            var httpClient = (await Fixture.GetSettings()).GetHttpClient();

            JObject rawMessage = new JObject();
            rawMessage["data"] = messageData["data"];
            rawMessage["encoding"] = messageData["encoding"];

            var request = new AblyRequest($"/channels/{channelName}/messages", HttpMethod.Post, Protocol.Json);
            request.RequestBody = rawMessage.ToJson().GetBytes();

            var client1 = await GetRestClient(protocol);
            await client1.AblyAuth.AddAuthHeader(request);
            await httpClient.Execute(request);

            var channel = client1.Channels.Get(channelName);
            var result = await channel.HistoryAsync();

            var returnedMessage = result.Items.First();
            var expectedType = (string) messageData["expectedType"];
            if (expectedType == "binary")
            {
                ((byte[]) returnedMessage.Data).ToHexString().Should().Be((string) messageData["expectedHexValue"]);
            }
            else
            {
                var returnedData = expectedType == "string" ? returnedMessage.Data.ToString() : returnedMessage.Data.ToJson();
                var expectedValue = expectedType == "string" ? (string)messageData["expectedValue"] : messageData["expectedValue"].ToJson();
                returnedData.Should().Be(expectedValue);
            }
        }

        [Theory]
        [InteropabilityMessagePayloadData]
        [Trait("spec", "RSL6a1")]
        public async Task WithTestMessagePayloadsWhenDecoding_ShouldEncodeMessagesAsPerSpec(Protocol protocol,
            JObject messageData)
        {
            var channelName = "channel-name-" + new Random().Next(int.MaxValue);
            var httpClient = (await Fixture.GetSettings()).GetHttpClient();
            var expectedType = (string)messageData["expectedType"];

            var client1 = await GetRestClient(protocol);
            var channel = client1.Channels.Get(channelName);

            //Act
            if(expectedType == "binary")
                await channel.PublishAsync("event", ((string)messageData["expectedHexValue"]).ToByteArray());
            else if (expectedType == "string")
                await channel.PublishAsync("event", (string)messageData["expectedValue"]);
            else
                await channel.PublishAsync("event", messageData["expectedValue"]);

            var request = new AblyRequest($"/channels/{channelName}/messages", HttpMethod.Get, Protocol.Json);
            await client1.AblyAuth.AddAuthHeader(request);
            var response = await httpClient.Execute(request);
            
            //Assert
            var historyData = JArray.Parse(response.TextResponse);
            var responseData = (JObject)historyData.First;

            if (expectedType == "binary")
                ((string)responseData["data"]).Should().Be((string)messageData["data"]);
            else if (expectedType == "json")
                responseData["data"].ToJson().Should().Be(messageData["data"].ToJson());
            else
            {
                ((string) responseData["data"]).Should().Be((string) messageData["data"]);
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL6b")]
        public async Task WithEncryptionCipherAlgorithmMismatch_ShouldLeaveMessageEncryptedAndLogError(Protocol protocol)
        {
            var loggerSink = new TestLoggerSink();
            var logger = new IO.Ably.Logger.InternalLogger(Defaults.DefaultLogLevel, loggerSink);
            
            
            var client = await GetRestClient(protocol);
            var channel1 = client.Channels.Get("persisted:encryption", GetOptions(examples));

            var payload = "test payload";
            await channel1.PublishAsync("test", payload);

            var channel2 = client.Channels.Get("persisted:encryption", new ChannelOptions(logger, true, new CipherParams(Crypto.GenerateRandomKey(128, CipherMode.CBC))));
            var message = (await channel2.HistoryAsync()).Items.First();

            loggerSink.LastLoggedLevel.Should().Be(LogLevel.Error);
            loggerSink.LastMessage.Should().Contain("Error decrypting payload");
            message.Encoding.Should().Be("utf-8/cipher+aes-128-cbc");
            
        }

        private object DecodeData(string data, string encoding)
        {
            if (encoding == "json")
            {
                return JsonHelper.Deserialize(data);
            }
            if (encoding == "base64")
                return data.FromBase64();

            return data;
        }
    }
}
