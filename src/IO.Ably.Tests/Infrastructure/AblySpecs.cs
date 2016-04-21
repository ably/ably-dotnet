using System;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class AblySpecs
    {
        public ITestOutputHelper Output { get; }
        public const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        public DateTimeOffset Now { get; set; }

        public AblySpecs(ITestOutputHelper output)
        {
            Now = Config.Now();
            Config.Now = () => Now;    
            Output = output;
        }
    }
}