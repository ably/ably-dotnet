using System;
using System.Threading.Tasks;

namespace DotnetPush.Infrastructure
{
    /// <summary>
    /// Helpers.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Executes an action after a specified interval in milliseconds.
        /// </summary>
        /// <param name="action">what to execute.</param>
        /// <param name="delayInMs">how many milliseconds to wait.</param>
        /// <returns>Async operation.</returns>
        public static async Task DelayAction(Func<Task> action, int delayInMs = 2000)
        {
            await Task.Delay(delayInMs);
            await action();
        }
    }
}
