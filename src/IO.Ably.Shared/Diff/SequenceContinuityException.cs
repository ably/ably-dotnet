using System;

namespace IO.Ably.Diff
{
    /// <summary>
    /// Thrown when <see cref="VcdiffDecoder"/>'s built-in sequence continuity check fails.
    /// </summary>
    internal class SequenceContinuityException : Exception
    {
        internal SequenceContinuityException(string expectedId, string actualId)
            : base($"Sequence continuity check failed - the provided id ({expectedId}) does not match the last preserved sequence id ({actualId})")
        {
        }
    }
}
