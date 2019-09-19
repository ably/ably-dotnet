using System;

namespace IO.Ably.Realtime.Workflow
{
    internal abstract class RealtimeCommand
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

        public string Name => GetType().Name;

        public string Explain()
        {
            var data = ExplainData();
            if (data.IsNotEmpty())
            {
                data = " Data: " + data;
            }

            return $"{GetType().Name}:{data} Meta:{Id}|{Created:s}";
        }

        protected abstract string ExplainData();

        public static RealtimeCommand Batch(params RealtimeCommand[] commands) => ListCommand.Create(commands);
    }
}