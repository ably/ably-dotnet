using System;

namespace IO.Ably.Tests
{
    public abstract class AblySpecs
    {
        public const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        private static DateTimeOffset _now = Config.Now();
        public DateTimeOffset Now => _now;
    }
}