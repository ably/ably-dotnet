using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Tests.Realtime
{
    public static class AblyRealtimeTestExtensions
    {
        public static void FakeProtocolMessageReceived(this AblyRealtime client, ProtocolMessage message)
        {
            client.Workflow.QueueCommand(ProcessMessageCommand.Create(message));
        }

        public static void FakeMessageReceived(this AblyRealtime client, Message message, string channel = null)
        {
            client.FakeProtocolMessageReceived(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message) { Messages = new[] { message }, Channel = channel });
        }
    }
}