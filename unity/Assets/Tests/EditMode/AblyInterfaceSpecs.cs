using System.Collections;
using System.Collections.Generic;
using Assets.Tests.AblySandbox;
using Cysharp.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Assets.Tests.EditMode
{
    [TestFixture]
    public class AblyInterfaceSpecs
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
            AblySandbox = new AblySandbox.AblySandbox(_sandboxFixture);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AblySandbox.Dispose();
            yield return null;
        }

        public AblySandbox.AblySandbox AblySandbox { get; set; }

        static Protocol[] _protocols = { Protocol.Json };

        [UnityTest]
        public IEnumerator TestConnection(
            [ValueSource(nameof(_protocols))] Protocol protocol) => UniTask.ToCoroutine(async () =>
        {
            var realtimeClient = await AblySandbox.GetRealtimeClient(protocol, (options, _) => options.AutoConnect = false);
            Assert.AreEqual(ConnectionState.Initialized, realtimeClient.Connection.State);

            // Check for active connection
            var connectionStates = new List<ConnectionState>();
            realtimeClient.Connection.On(change =>
            {
                connectionStates.Add(change.Current);
            });
            realtimeClient.Connect();
            await realtimeClient.WaitForState(ConnectionState.Connected);
            Assert.AreEqual(ConnectionState.Connecting, connectionStates[0]);
            Assert.AreEqual(ConnectionState.Connected, connectionStates[1]);

            // Check for connection close
            connectionStates.Clear();
            realtimeClient.Close();
            await realtimeClient.WaitForState(ConnectionState.Closed);
            Assert.AreEqual(ConnectionState.Closing, connectionStates[0]);
            Assert.AreEqual(ConnectionState.Closed, connectionStates[1]);
        });

        [UnityTest]
        public IEnumerator TestChannel(
            [ValueSource(nameof(_protocols))] Protocol protocol) => UniTask.ToCoroutine(async () =>
        {
            var realtimeClient = await AblySandbox.GetRealtimeClient(protocol);
            await realtimeClient.WaitForState(ConnectionState.Connected);

            var channel = realtimeClient.Channels.Get("TestChannel");
            var channelStates = new List<ChannelState>();
            channel.On(change =>
            {
                channelStates.Add(change.Current);
            });

            channel.Attach();
            await channel.WaitForState();
            Assert.AreEqual(ChannelState.Attaching, channelStates[0]);
            Assert.AreEqual(ChannelState.Attached, channelStates[1]);

            channelStates.Clear();
            channel.Detach();
            await channel.WaitForState(ChannelState.Detached);
            Assert.AreEqual(ChannelState.Detaching, channelStates[0]);
            Assert.AreEqual(ChannelState.Detached, channelStates[1]);
        });

    }
}