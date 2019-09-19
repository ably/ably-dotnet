namespace IO.Ably.Realtime.Workflows
{
    internal interface IQueueCommand
    {
        void QueueCommand(params RealtimeCommand[] commands);
    }
}