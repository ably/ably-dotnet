using System;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    public static class TestExtensions
    {
        internal static TestTransportWrapper GetTestTransport(this IRealtimeClient client)
        {
            return ((AblyRealtime)client).ConnectionManager.Transport as TestTransportWrapper;
        }

        internal static void SetOnTransportCreated(this IRealtimeClient client, Action<TestTransportWrapper> onCreated)
        {
            var factory = ((AblyRealtime)client).Options.TransportFactory as TestTransportFactory;
            factory.OnTransportCreated = onCreated;
        }

        internal static void BlockActionFromSending(this IRealtimeClient client, ProtocolMessage.MessageAction action)
        {
            var transport = ((AblyRealtime)client).ConnectionManager.Transport as TestTransportWrapper;
            if (transport is null)
            {
                throw new Exception("Client is not using test transport so you can't add BlockedActions");
            }

            transport.BlockSendActions.Add(action);
        }

        internal static void SimulateLostConnectionAndState(this IRealtimeClient client)
        {
            client.Connection.Id = string.Empty;
            client.Connection.Key = "xxxxx!xxxxxxx-xxxxxxxx-xxxxxxxx";
            client.GetTestTransport().Close(false);
        }

        internal static void BeforeProtocolMessageProcessed(this AblyRealtime client, Action<ProtocolMessage> action)
        {
            var t = client.GetTestTransport();
            if (t != null)
            {
                t.BeforeDataProcessed = action;
            }

            if (client.Options.TransportFactory is TestTransportFactory f)
            {
                f.BeforeDataProcessed = action;
            }
        }

        internal static void AfterProtocolMessageProcessed(this AblyRealtime client, Action<ProtocolMessage> action)
        {
            client.GetTestTransport().AfterDataReceived = action;
            (client.Options.TransportFactory as TestTransportFactory).AfterDataReceived = action;
        }
    }
}
