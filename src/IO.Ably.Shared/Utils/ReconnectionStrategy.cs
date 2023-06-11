using System;

namespace IO.Ably.Shared.Utils
{
    internal class ReconnectionStrategy
    {
        private static readonly Random Random = new Random();

        public static double GetBackoffCoefficient(int count)
        {
            return Math.Min((count + 2) / 3d, 2d);
        }

        public static double GetJitterCoefficient()
        {
            return 1 - (Random.NextDouble() * 0.2);
        }

        public static double GetRetryTime(double initialTimeout, int retryAttempt)
        {
            return initialTimeout * GetBackoffCoefficient(retryAttempt) * GetJitterCoefficient();
        }
    }
}
