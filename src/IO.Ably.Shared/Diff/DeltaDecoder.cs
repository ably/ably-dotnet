using System;
using System.IO;

namespace IO.Ably.Diff
{
    /// <summary>
    /// VCDIFF decoder capable of processing continuous sequences of consecutively generated VCDIFFs.
    /// </summary>
    internal class DeltaDecoder
    {
        private byte[] _base;
        private string _baseId;

        /// <summary>
        /// Checks if <paramref name="data"/> contains valid VCDIFF.
        /// </summary>
        /// <param name="data">The data to be checked (byte[] or Base64-encoded string).</param>
        /// <returns>True if <paramref name="data"/> contains valid VCDIFF, false otherwise.</returns>
        public static bool IsDelta(byte[] data)
        {
            return HasVcdiffHeader(data);
        }

        /// <summary>
        /// Applies the <paramref name="delta"/> to the result of applying the previous delta or to the base data if no previous delta has been applied yet.
        /// Base data has to be set by <see cref="SetBase(byte[], string)"/> before calling this method for the first time.
        /// </summary>
        /// <param name="delta">The delta to be applied.</param>
        /// <param name="deltaId">(Optional) Sequence ID of the current delta application result. If set, it will be used for sequence continuity check during the next delta application.</param>
        /// <param name="baseId">(Optional) Sequence ID of the expected previous delta application result. If set, it will be used to perform sequence continuity check agains the last preserved sequence ID.</param>
        /// <returns><see cref="DeltaApplicationResult"/> instance.</returns>
        /// <exception cref="InvalidOperationException">The decoder is not initialized by calling <see cref="SetBase(object, string)"/>.</exception>
        /// <exception cref="SequenceContinuityException">The provided <paramref name="baseId"/> does not match the last preserved sequence ID.</exception>
        /// <exception cref="ArgumentException">The provided <paramref name="delta"/> is not a valid VCDIFF.</exception>
        /// <exception cref="DeltaCodec.Vcdiff.VcdiffFormatException">.</exception>
        public DeltaApplicationResult ApplyDelta(byte[] delta, string deltaId = null, string baseId = null)
        {
            if (_base == null)
            {
                throw new InvalidOperationException($"Uninitialized decoder - {nameof(SetBase)}() should be called first");
            }

            if (_baseId != baseId)
            {
                throw new SequenceContinuityException(baseId, _baseId);
            }

            if (HasVcdiffHeader(delta) == false)
            {
                throw new ArgumentException($"The provided {nameof(delta)} is not a valid VCDIFF delta");
            }

            var result = ApplyDelta(_base, delta);
            _base = result.AsByteArray();
            _baseId = deltaId;
            return result;
        }

        public static DeltaApplicationResult ApplyDelta(byte[] @base, byte[] delta)
        {
            using (MemoryStream baseStream = new MemoryStream(@base))
            using (MemoryStream deltaStream = new MemoryStream(delta))
            using (MemoryStream decodedStream = new MemoryStream())
            {
                Vcdiff.VcdiffDecoder.Decode(baseStream, deltaStream, decodedStream);

                return new DeltaApplicationResult(decodedStream.ToArray());
            }
        }

        /// <summary>
        /// Sets the base object used for the next delta application (see <see cref="ApplyDelta(object, string, string)"/>).
        /// </summary>
        /// <param name="newBase">The base object to be set.</param>
        /// <param name="newBaseId">(Optional) The <paramref name="newBase"/>'s sequence ID, to be used for sequence continuity checking when delta is applied using <see cref="ApplyDelta(object, string, string)"/>.</param>
        /// <exception cref="ArgumentNullException">The provided <paramref name="newBase"/> parameter is null.</exception>
        public void SetBase(byte[] newBase, string newBaseId = null)
        {
            if (newBase == null)
            {
                throw new ArgumentNullException($"{nameof(newBase)} cannot be null");
            }

            _base = newBase;
            _baseId = newBaseId;
        }

        private static bool HasVcdiffHeader(byte[] delta)
        {
            return delta[0] == 0xd6 &&
                   delta[1] == 0xc3 &&
                   delta[2] == 0xc4 &&
                   delta[3] == 0;
        }
    }
}
