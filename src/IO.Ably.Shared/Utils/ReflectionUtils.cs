using System;
using System.Reflection;

namespace IO.Ably.Utils
{
    internal static class ReflectionUtils
    {
        // TODO: consider removing this class, it doesn't seem to be used
        [Obsolete("This method is not used and should be removed")]
#pragma warning disable SA1300 // Element must begin with upper-case letter
        public static bool isPropsEqual<T>(T a, T b)
#pragma warning restore SA1300 // Element must begin with upper-case letter
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