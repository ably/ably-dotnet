using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class PublishAndHistoryTests
    {
        private static Ably.Rest GetAbly()
        {
            var testData = TestsSetup.TestData;
            
            var options = new AblyOptions
            {
                Key = testData.keys.First().keyStr,
                Tls = true
            };
            var ably = new Rest(options);
            return ably;
        }
        [Test]
        public void CanPublishAMessageAndRetrieveIt()
        {
            Ably.Rest ably = GetAbly();
            IChannel channel = ably.Channels.Get("persisted:test");
            channel.Publish("test", true);

            Thread.Sleep(16000);

            var messages = channel.History();

            Assert.AreEqual(1, messages.Count());
            Assert.True(messages.First().Value<bool>());
        }

        [Test]
        public void CanPublishWithVariousDataTypesAndRetrieveCorrectMessages()
        {
            var ably = GetAbly();
            IChannel publish = ably.Channels.Get("persisted:test");
            var time = ably.Time();
            publish.Publish("publish0", true);
            publish.Publish("publish1", 24);
            publish.Publish("publish2", 24.0);
            publish.Publish("publish3", "This is a string message payload");
            byte[] byteMessage = UTF8Encoding.UTF8.GetBytes("This is a byte[] message payload");
            publish.Publish("publish4", byteMessage);
            var obj = new { test = "This is a json object message payload" };
            publish.Publish("publish5", obj);
            List<int> listOfValues = new List<int> { 1, 2, 3 };
            publish.Publish("publish6", listOfValues);

            Thread.Sleep(8000);

            var messages = publish.History(new HistoryDataRequestQuery { Start = time, Direction = QueryDirection.Forwards}).ToList();

            Assert.AreEqual(7, messages.Count());
            Assert.AreEqual(true, messages[0].Value<bool>());
            Assert.AreEqual(24, messages[1].Value<int>());
            Assert.AreEqual(24.0, messages[2].Value<double>());
            Assert.AreEqual("This is a string message payload", messages[3].Value<string>());
            Assert.AreEqual(byteMessage, messages[4].Value<byte[]>());
            Assert.AreEqual(true, messages[4].IsBinaryMessage);
            Assert.AreEqual(obj, messages[5].Value(obj.GetType()));
            Assert.AreEqual(listOfValues, messages[6].Value<List<int>>());
        }
    }
}
