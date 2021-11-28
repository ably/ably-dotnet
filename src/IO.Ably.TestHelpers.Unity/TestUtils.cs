using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IO.Ably.TestHelpers.Unity
{
    public static class ResourceHelper
    {
        public static string GetResource(string localResName)
        {
            Assembly ass = typeof(ResourceHelper).Assembly;
            string defaultNamespace = ass.GetName().Name;
            string resName = $"{defaultNamespace}.{localResName}";
            Stream resourceStream = ass.GetManifestResourceStream(resName);
            if (resourceStream == null)
            {
                throw new Exception("Resource not found: " + resName);
            }

            using var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }

        public static byte[] GetBinaryResource(string localResName)
        {
            Assembly ass = typeof(ResourceHelper).Assembly;
            string defaultNamespace = ass.GetName().Name;
            string resName = $"{defaultNamespace}.{localResName}";
            using Stream resourceStream = ass.GetManifestResourceStream(resName);
            if (resourceStream == null)
            {
                throw new Exception("Resource not found: " + resName);
            }

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);
            return data;
        }
    }

    public static class DateHelper
    {
        public static DateTimeOffset CreateDate(int year, int month, int day, int hours = 0, int minutes = 0, int seconds = 0)
        {
            return new DateTimeOffset(year, month, day, hours, minutes, seconds, TimeSpan.Zero);
        }
    }

    internal static class TestHelpers
    {
        public static DateTimeOffset Now()
        {
            return NowFunc()();
        }

        public static Func<DateTimeOffset> NowFunc()
        {
            return Defaults.NowFunc();
        }

        public static async Task WaitFor(int timeoutMs, int taskCount, Action<Action> action, Action onFail = null)
        {
            var tsc = new TaskCompletionAwaiter(timeoutMs, taskCount);

            void Done()
            {
                tsc.Tick();
            }

            action(Done);
            var success = await tsc.Task;
            if (!success)
            {
                var msg = $"Timeout of {timeoutMs}ms exceeded.";
                if (taskCount > 1)
                {
                    msg += $" Completed {taskCount - tsc.TaskCount} of {taskCount} tasks.";
                }

                onFail?.Invoke();

                throw new Exception(msg);
            }
        }
    }
}
