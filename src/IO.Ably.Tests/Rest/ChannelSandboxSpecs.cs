﻿using System.Linq;
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
            message.name = "large";
            message.data = new string('a', 50 * 1024 * 8); // 100KB
            var client = await GetRestClient(protocol);
            var ex = await Assert.ThrowsAsync<AblyException>(()
                => client.Channels.Get("large").Publish(message));

            Output.WriteLine("Error: " + ex.Message);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1f1")]
        public async Task WithBasicAuthWhenMessageHasClientId_ShouldRetrieveMessageWithSameClientId(Protocol protocol)
        {
            var message = new Message("test", "test") {clientId = "123"};
            var client = await GetRestClient(protocol);
            var channel = client.Channels.Get("persisted:test");
            await channel.Publish(message);

            var result = await channel.History();
            result.First().clientId.Should().Be("123");
        }

        //RSL1g
        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1g1b")]
        public async Task WithImplicitClientIdComingFromOptions_ReturnsMessageWithCorrectClientId(Protocol protocol)
        {
            var message = new Message("test", "test") { clientId = null};
            var client = await GetRestClient(protocol, opts => opts.ClientId = "999");
            var channel = client.Channels.Get("persisted:test".AddRandomSuffix());
            await channel.Publish(message);

            var result = await channel.History();
            result.First().clientId.Should().Be("999");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1g2")]
        public async Task WithExplicitClientIdMatchingClientIdInOptions_ReturnsMessageWithCorrectClientId(Protocol protocol)
        {
            var message = new Message("test", "test") { clientId = "999" };
            var client = await GetRestClient(protocol, opts => opts.ClientId = "999");
            var channel = client.Channels.Get("persisted:test".AddRandomSuffix());
            await channel.Publish(message);

            var result = await channel.History();
            result.First().clientId.Should().Be("999");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL1g3")]
        [Trait("spec", "RSL1g4")]
        public async Task WithExplicitClientIdNotMatchingClientIdInOptions_ReturnsMessageWithCorrectClientId(Protocol protocol)
        {
            var message = new Message("test", "test") { clientId = "999" };
            var client = await GetRestClient(protocol, opts => opts.ClientId = "111");
            var channel = client.Channels.Get("test");
            var ex = await Assert.ThrowsAsync<AblyException>(() => channel.Publish(message));
            Output.WriteLine("Error: " + ex.Message);

            //Can publish further messages in the same channel
            await channel.Publish("test", "test");
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
            IChannel channel = ably.Channels.Get("persisted:test".AddRandomSuffix(), GetOptions(examples));
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
        [Trait("spec", "RSL5b")]
        [Trait("spec", "RSL5c")]
        //Uses the to publish the examples inside crypto-data-256.json to publish and then retrieve the messages
        public async Task CanPublishAMessageAndRetrieveIt256(Protocol protocol)
        {
            var items = (JArray)examples256["items"];

            AblyRest ably = await GetRestClient(protocol);
            IChannel channel = ably.Channels.Get("persisted:test".AddRandomSuffix(), GetOptions(examples256));
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

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL4a")]
        public async Task WithUnsupportedPayloadTypes_ShouldRaiseException(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var channel = client.Channels.Get("persisted:test_" + protocol);

            var ex = await Assert.ThrowsAsync<AblyException>(() => channel.Publish("int", 1));
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

            using (Logger.SetTempDestination(loggerSink))
            {
                var client = await GetRestClient(protocol);
                var channel1 = client.Channels.Get("persisted:encryption", GetOptions(examples));

                var payload = "test payload";
                await channel1.Publish("test", payload);

                var channel2 = client.Channels.Get("persisted:encryption", new ChannelOptions(true));
                var message = (await channel2.History()).First();

                loggerSink.LastLoggedLevel.Should().Be(LogLevel.Error);
                message.encoding.Should().Be("utf-8/cipher+aes-128-cbc");
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL6b")]
        public async Task WithEncryptionCipherAlgorithmMismatch_ShouldLeaveMessageEncryptedAndLogError(Protocol protocol)
        {
            var loggerSink = new TestLoggerSink();

            using (Logger.SetTempDestination(loggerSink))
            {
                var client = await GetRestClient(protocol);
                var channel1 = client.Channels.Get("persisted:encryption", GetOptions(examples));

                var payload = "test payload";
                await channel1.Publish("test", payload);

                var channel2 = client.Channels.Get("persisted:encryption", new ChannelOptions(true, new CipherParams(Crypto.GetRandomKey(CipherMode.CBC, 128))));
                var message = (await channel2.History()).First();

                loggerSink.LastLoggedLevel.Should().Be(LogLevel.Error);
                loggerSink.LastMessage.Should().Contain("Error decrypting payload");
                message.encoding.Should().Be("utf-8/cipher+aes-128-cbc");
            }
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
