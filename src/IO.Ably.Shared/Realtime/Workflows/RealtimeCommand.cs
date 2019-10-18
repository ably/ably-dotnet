using System;

namespace IO.Ably.Realtime.Workflow
{
    internal abstract class RealtimeCommand
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

        public string Name => GetType().Name;

        public string TriggeredByMessage { get; private set; } = string.Empty;

        public string Explain()
        {
            var data = ExplainData();
            if (data.IsNotEmpty())
            {
                data = " Data: " + data;
            }

            return $"{GetType().Name}:{data} Meta:{Id}|{Created:s}|TriggeredBy: {TriggeredByMessage}";
        }

        public void RecordTrigger(RealtimeCommand trigger)
        {
            TriggeredByMessage += $"{trigger.Name}:{trigger.Id}";
        }

        protected abstract string ExplainData();

        public override string ToString()
        {
            return $"{Name}: {Explain()}";
        }
    }

    internal static class RealtimeCommandExtensions
    {
        public static T TriggeredBy<T>(this T command, RealtimeCommand triggerCommand)
            where T : RealtimeCommand
        {
            command.RecordTrigger(triggerCommand);
            return command;
        }
    }
}
