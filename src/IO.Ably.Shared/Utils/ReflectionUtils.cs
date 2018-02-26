using System.Reflection;

namespace IO.Ably.Utils
{
    static class ReflectionUtils
    {
        public static bool isPropsEqual<T>(T a, T b)
        {
            // TODO [low]: optimize performance by compiling an expression instead of using runtime reflection.
            foreach (PropertyInfo pi in typeof(T).GetRuntimeProperties())
            {
                if (pi.IsSpecialName)
                {
                    continue;
                }

                if (!pi.CanRead)
                {
                    continue;
                }

                object x = pi.GetValue(a), y = pi.GetValue(b);
                if (x != y)
                {
                    return false;
                }
            }

            return true;
        }
    }
}