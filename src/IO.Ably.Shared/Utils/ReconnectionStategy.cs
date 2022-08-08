using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably.Shared.Utils
{
    internal class ReconnectionStategy
    {
        public static double GetBackoffCoefficient(int retryCount)
        {
            return Math.Min((retryCount + 2) / 3, 2);
        }

        public static double GetJitterCoefficient()
        {
            return 1 - (new Random().NextDouble() * 0.2);
        }

        public static double GetRetryTime(double initValue, int retryCount)
        {
            return initValue * GetBackoffCoefficient(retryCount) * GetJitterCoefficient();
        }
    }
}
