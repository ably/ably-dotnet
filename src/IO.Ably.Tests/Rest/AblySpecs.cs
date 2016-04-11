using System;

namespace IO.Ably.Tests
{
    public abstract class AblySpecs
    {
        public DateTimeOffset Now => Config.Now();
    }
}