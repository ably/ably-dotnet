using System;
using System.Buffers;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    /// <summary>
    /// Simple IBufferWriter implementation compatible with .NET Standard 2.0 and .NET Framework.
    /// This is used in tests as a replacement for ArrayBufferWriter which is not available in .NET Framework.
    /// </summary>
    internal class SimpleBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[256];
        private int _index = 0;

        public ReadOnlyMemory<byte> WrittenMemory => new ReadOnlyMemory<byte>(_buffer, 0, _index);

        public ReadOnlySpan<byte> WrittenSpan => new ReadOnlySpan<byte>(_buffer, 0, _index);

        public void Advance(int count)
        {
            _index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Memory<byte>(_buffer, _index, _buffer.Length - _index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Span<byte>(_buffer, _index, _buffer.Length - _index);
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (_index + sizeHint > _buffer.Length)
            {
                var newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
                var newBuffer = new byte[newSize];
                Array.Copy(_buffer, newBuffer, _index);
                _buffer = newBuffer;
            }
        }
    }
}
