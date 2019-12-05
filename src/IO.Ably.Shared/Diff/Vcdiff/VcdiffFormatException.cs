using System;

namespace IO.Ably.Diff.Vcdiff
{
    internal class VcdiffFormatException : Exception
    {
        internal VcdiffFormatException(string message)
            : base(message)
        {
        }

        internal VcdiffFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
