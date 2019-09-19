namespace IO.Ably.Realtime.Workflow
{
    internal interface IQueueCommand
    {
        void QueueCommand(params RealtimeCommand[] commands);
    }
}