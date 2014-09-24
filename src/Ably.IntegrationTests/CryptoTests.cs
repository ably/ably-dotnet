using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class CryptoTests
    {

        private Rest _ably;
        private Rest _ably2;

        [SetUp]
        public void Setup()
        {
            TestVars testVars = TestsSetup.TestData;
            var options = new AblyOptions();
            options.Host = testVars.restHost;
            options.Port = testVars.restPort;
            options.Tls = testVars.tls;
            options.Key = testVars.keys.First().keyStr;
            options.UseTextProtocol = true;
            _ably = new Rest(options);
            _ably2 = new Rest(options);
        }

        /**
         * Publish events with data of various datatypes using text protocol
         */
        [Test]
        public void CanPublishEncryptedMessages_WithDefaultEncryptionOptions()
        {
            /* first, publish some messages */
            IChannel channel;
            channel = _ably.Channels.Get("persisted:crypto_publish_text", new ChannelOptions() { Encrypted = true });

            channel.Publish("publish0", true);
            channel.Publish("publish1", 24);
            channel.Publish("publish2", 24.234);
            channel.Publish("publish3", "This is a string message payload");
            channel.Publish("publish4", "This is a byte[] message payload".GetBytes());
            var jsonObj = new JObject();
            jsonObj.Add("test", "This is a JSONObject message payload");
            channel.Publish("publish5", jsonObj);
            var jsonArray = new JArray();
            jsonArray.Add("This is a JSONArray message payload");
            channel.Publish("publish6", jsonArray);

            Thread.Sleep(16000);


            var messages = channel.History();
            Assert.NotNull(messages);
            Assert.AreEqual(7, messages.Count());
            var messageContents = new Dictionary<String, Object>();
            /* verify message contents */

            foreach (var message in messages)
                messageContents.Add(message.Name, message.Data);

            Assert.AreEqual(true, messageContents["publish0"]);
            Assert.AreEqual(24, messageContents["publish1"]);
            Assert.AreEqual(24.234, messageContents["publish2"]);
            Assert.AreEqual("This is a string message payload", messageContents["publish3"]);
            Assert.AreEqual("This is a byte[] message payload", ((byte[])messageContents["publish4"]).GetText());
            Assert.AreEqual(((JObject)messageContents["publish5"]).ToString(), "{\"test\":\"This is a JSONObject message payload\"}");
            Assert.AreEqual(((JArray)messageContents["publish6"]).ToString(), "[\"This is a JSONArray message payload\"]");
        }


        /**
         * Publish events with data of various datatypes using text protocol with a 256-bit key
         */
        //[Test]
        //public void Publish_WithCustomKey() {
        //    /* first, publish some messages */
        //    Channel publish0;
        //    try {
        //        /* create a key */
        //        KeyGenerator keygen = KeyGenerator.getInstance("AES");
        //        keygen.init(256);
        //        byte[] key = keygen.generateKey().getEncoded();
        //        final CipherParams params = Crypto.GetDefaultParams();

        //        /* create a channel */
        //        ChannelOptions channelOpts = new ChannelOptions() {{ encrypted = true; this.cipherParams = params; }};
        //        publish0 = ably_text.channels.get("persisted:crypto_publish_text_256", channelOpts);

        //        publish0.publish("publish0", new Boolean(true));
        //        publish0.publish("publish1", new Integer(24));
        //        publish0.publish("publish2", new Double(24.234));
        //        publish0.publish("publish3", "This is a string message payload");
        //        publish0.publish("publish4", "This is a byte[] message payload".getBytes());
        //        JSONObject jsonObj = new JSONObject();
        //        jsonObj.put("test", "This is a JSONObject message payload");
        //        publish0.publish("publish5", jsonObj);
        //        JSONArray jsonArray = new JSONArray();
        //        jsonArray.put(0, "This is a JSONArray message payload");
        //        publish0.publish("publish6", jsonArray);
        //    } catch(AblyException e) {
        //        e.printStackTrace();
        //        fail("channelpublish_text: Unexpected exception");
        //        return;
        //    } catch (JSONException e) {
        //        e.printStackTrace();
        //        fail("channelpublish_text: Unexpected exception");
        //        return;
        //    } catch (NoSuchAlgorithmException e) {
        //        e.printStackTrace();
        //        fail("init0: Unexpected exception generating key");
        //        return;
        //    }
        //    /* wait for the history to be persisted */
        //    try {
        //        Thread.sleep(16000);
        //    } catch(InterruptedException ie) {}
        //    /* get the history for this channel */
        //    try {
        //        PaginatedResult<Message[]> messages = publish0.history(null);
        //        assertNotNull("Expected non-null messages", messages);
        //        assertEquals("Expected 7 messages", messages.current.length, 7);
        //        HashMap<String, Object> messageContents = new HashMap<String, Object>();
        //        /* verify message contents */
        //        for(Message message : messages.current)
        //            messageContents.put(message.name, message.data);
        //        assertEquals("Expect publish0 to be Boolean(true)", messageContents.get("publish0"), new Boolean(true));
        //        assertEquals("Expect publish1 to be Integer(24)", messageContents.get("publish1"), new Integer(24));
        //        assertEquals("Expect publish2 to be Double(24.234)", messageContents.get("publish2"), new Double(24.234));
        //        assertEquals("Expect publish3 to be expected String", messageContents.get("publish3"), "This is a string message payload");
        //        assertEquals("Expect publish4 to be expected byte[]", new String((byte[])messageContents.get("publish4")), "This is a byte[] message payload");
        //        assertEquals("Expect publish5 to be expected JSONObject", ((JSONObject)messageContents.get("publish5")).toString(), "{\"test\":\"This is a JSONObject message payload\"}");
        //        assertEquals("Expect publish6 to be expected JSONArray", ((JSONArray)messageContents.get("publish6")).toString(), "[\"This is a JSONArray message payload\"]");
        //    } catch (AblyException e) {
        //        e.printStackTrace();
        //        fail("channelpublish_text: Unexpected exception");
        //        return;
        //    }
        //}


        /**
         * Connect twice to the service, using different cipher keys.
         * Publish an encrypted message on that channel using
         * the default cipher params and verify that the decrypt failure
         * is noticed as bad recovered plaintext.
         */
        [Test]
        public void PublishEncryptedMessageAndTryToReadItWithADifferentKey()
        {
            /* first, publish some messages */
            IChannel tx_publish;
            /* create a channel */
            ChannelOptions tx_channelOpts = new ChannelOptions() { Encrypted = true };
            tx_publish = _ably2.Channels.Get("persisted:crypto_publish_key_mismatch", tx_channelOpts);

            tx_publish.Publish("publish0", true);
            tx_publish.Publish("publish1", 24);
            tx_publish.Publish("publish2", 24.234);
            tx_publish.Publish("publish3", "This is a string message payload");
            tx_publish.Publish("publish4", "This is a byte[] message payload".GetBytes());
            JObject jsonObj = new JObject();
            jsonObj.Add("test", "This is a JSONObject message payload");
            tx_publish.Publish("publish5", jsonObj);
            JArray jsonArray = new JArray();
            jsonArray.Add("This is a JSONArray message payload");
            tx_publish.Publish("publish6", jsonArray);

            /* wait for the history to be persisted */
            Thread.Sleep(16000);
            /* get the history for this channel */

            Assert.Throws<AblyException>(
                delegate
                {
                    ChannelOptions rx_channelOpts = new ChannelOptions() { Encrypted = true };
                    IChannel rx_publish = _ably.Channels.Get("persisted:crypto_publish_key_mismatch", rx_channelOpts);
                    rx_publish.History(null);
                });
        }

        /**
         * Connect twice to the service, one with and one without encryption.
         * Publish an unencrypted message and verify that the receiving connection
         * does not attempt to decrypt it.
         */
        [Test]
        public void PublishUnEncryptedMessageAndReadItWithEncryptedChannel()
        {
            /* first, publish some messages */

            /* create a channel */
            IChannel tx_publish = _ably.Channels.Get("persisted:crypto_send_unencrypted");

            tx_publish.Publish("publish0", true);
            tx_publish.Publish("publish1", 24);
            tx_publish.Publish("publish2", 24.234);
            tx_publish.Publish("publish3", "This is a string message payload");
            tx_publish.Publish("publish4", "This is a byte[] message payload".GetBytes());
            JObject jsonObj = new JObject();
            jsonObj.Add("test", "This is a JSONObject message payload");
            tx_publish.Publish("publish5", jsonObj);
            JArray jsonArray = new JArray();
            jsonArray.Add("This is a JSONArray message payload");
            tx_publish.Publish("publish6", jsonArray);

            /* wait for the history to be persisted */
            Thread.Sleep(16000);

            ChannelOptions channelOpts = new ChannelOptions() { Encrypted = true };
            IChannel rx_publish = _ably.Channels.Get("persisted:crypto_send_unencrypted", channelOpts);

            var messages = rx_publish.History(null);
            Assert.NotNull(messages);
            Assert.AreEqual(7, messages.Count());
            var messageContents = new Dictionary<String, Object>();
            /* verify message contents */
            foreach (var message in messages)
                messageContents.Add(message.Name, message.Data);

            Assert.AreEqual(true, messageContents["publish0"]);
            Assert.AreEqual(24, messageContents["publish1"]);
            Assert.AreEqual(24.234, messageContents["publish2"]);
            Assert.AreEqual("This is a string message payload", messageContents["publish3"]);
            Assert.AreEqual("This is a byte[] message payload", ((byte[])messageContents["publish4"]).GetText());
            Assert.AreEqual("{\"test\":\"This is a JSONObject message payload\"}", messageContents["publish5"].ToString());
            Assert.AreEqual("[\"This is a JSONArray message payload\"]", messageContents["publish6"].ToString());
        }

        /**
         * Connect twice to the service, one with and one without encryption.
         * Publish an unencrypted message and verify that the receiving connection
         * does not attempt to decrypt it.
         */

        [Test]
        public void PublishFromEncryptedChannelAndReadFromUnEncrypted()
        {
            /* first, publish some messages */
            /* create a channel */
            ChannelOptions channelOpts = new ChannelOptions() { Encrypted = true };
            IChannel tx_publish = _ably.Channels.Get("persisted:crypto_send_encrypted_unhandled", channelOpts);

            tx_publish.Publish("publish1", 24);
            tx_publish.Publish("publish2", 24.234);
            tx_publish.Publish("publish3", "This is a string message payload");
            tx_publish.Publish("publish4", "This is a byte[] message payload".GetBytes());
            var jsonObj = new JObject();
            jsonObj.Add("test", "This is a JSONObject message payload");
            tx_publish.Publish("publish5", jsonObj);
            JArray jsonArray = new JArray();
            jsonArray.Add("This is a JSONArray message payload");
            tx_publish.Publish("publish6", jsonArray);
            /* wait for the history to be persisted */
            Thread.Sleep(16000);
            /* get the history for this channel */

            IChannel rx_publish = _ably.Channels.Get("persisted:crypto_send_encrypted_unhandled");

            var messages = rx_publish.History(null);
            Assert.NotNull(messages);
            Assert.AreEqual(6, messages.Count());
            var messageContents = new Dictionary<String, Object>();
            /* verify message contents */
            foreach (Message message in messages)
                messageContents.Add(message.Name, message.Data);

            Assert.IsInstanceOf<CipherData>(messageContents["publish1"]);
            Assert.IsInstanceOf<CipherData>(messageContents["publish2"]);
            Assert.IsInstanceOf<CipherData>(messageContents["publish3"]);
            Assert.IsInstanceOf<CipherData>(messageContents["publish4"]);
            Assert.IsInstanceOf<CipherData>(messageContents["publish5"]);
            Assert.IsInstanceOf<CipherData>(messageContents["publish6"]);
        }
    }
}
