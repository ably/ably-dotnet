using System.Collections;
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
        });

    }
}