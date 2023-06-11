using System;

namespace IO.Ably.Shared.Utils
{
    internal class ReconnectionStrategy
    {
        private static readonly Random Random = new Random();

        public static double GetBackoffCoefficient(int retryCount)
        {
            return Math.Min((retryCount + 2) / 3, 2);
        }

        public static double GetJitterCoefficient()
        {
            return 1 - (Random.NextDouble() * 0.2);
        }

        public static double GetRetryTime(double initValue, int retryCount)
        {
            return initValue * GetBackoffCoefficient(retryCount) * GetJitterCoefficient();
        }
    }
}
