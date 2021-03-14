using System;
using System.IO;

namespace JwsDetachedStreaming
{
    public class CompositeReadStream : Stream
    {
        private readonly byte[][] _buffers;
        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        private int _bufferIndex = 0;
        private int _bufferPosition = 0;

        public CompositeReadStream(byte[][] buffers, Stream stream, bool leaveOpen = false)
        {
            _buffers = buffers;
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var totalBytesRead = 0;

            while (count > 0 && _bufferIndex < _buffers.Length)
            {
                var currentBuffer = _buffers[_bufferIndex];

                var n = currentBuffer.Length - _bufferPosition;
                if (n > count)
                {
                    n = count;
                }

                if (n <= 0)
                {
                    _bufferIndex++;
                    _bufferPosition = 0;
                    continue;
                }

                if (n <= 8)
                {
                    var byteCount = n;
                    while (--byteCount >= 0)
                        buffer[offset + byteCount] = currentBuffer[_bufferPosition + byteCount];
                }
                else
                {
                    Buffer.BlockCopy(currentBuffer, _bufferPosition, buffer, offset, n);
                }

                totalBytesRead += n;
                _bufferPosition += n;
                offset += n;
                count -= n;
            }

            if (count > 0)
            {
                totalBytesRead += _stream.Read(buffer, offset, count);
            }

            return totalBytesRead;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_leaveOpen)
                {
                    _stream.Dispose();
                }
            }
        }
    }
}
