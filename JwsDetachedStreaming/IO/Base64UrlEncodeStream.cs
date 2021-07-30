using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JwsDetachedStreaming.IO
{
    public class Base64UrlEncodeStream : Stream
    {
        private readonly Stream _stream;
        
        public Base64UrlEncodeStream(Stream stream)
        {
            _stream = stream;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }
        
        private readonly byte[] _remaining = new byte[3];
        private int _remainingSize = 0;

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            if (_remainingSize > 0)
            {
                var array = new byte[_remainingSize + buffer.Length];
                Buffer.BlockCopy(_remaining, 0, array, 0, _remainingSize);

                buffer.CopyTo(array.AsMemory(_remainingSize));
                
                _remainingSize = 0;
                
                buffer = new ReadOnlyMemory<byte>(array);
            }

            var output = new byte[Base64Url.GetMaxEncodedLength(buffer.Length)];

            var status = Base64Url.Encode(buffer.Span, output, out var bytesConsumed, out var bytesWritten, false);
            switch (status)
            {
                case OperationStatus.Done:
                    break;

                case OperationStatus.NeedMoreData:
                    for (var index = bytesConsumed; index < buffer.Length; index++)
                    {
                        _remaining[_remainingSize] = buffer.Span[index];
                        _remainingSize++;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (bytesWritten > 0)
            {
                return _stream.WriteAsync(new ReadOnlyMemory<byte>(output, 0, bytesWritten), cancellationToken);
            }
            else
            {
                return new ValueTask();
            }
        }

        public override ValueTask DisposeAsync()
        {
            if (_remainingSize > 0)
            {
                var input = new ReadOnlySpan<byte>(_remaining, 0, _remainingSize);
                var output = new byte[Base64Url.GetMaxEncodedLength(_remainingSize)];
                _remainingSize = 0;

                var status = Base64Url.Encode(input, output, out _, out var bytesWritten, true);
                if (status != OperationStatus.Done)
                {
                    throw new FormatException(status.ToString());
                }

                return _stream.WriteAsync(new ReadOnlyMemory<byte>(output, 0, bytesWritten), default);
            }

            return new ValueTask();
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position 
        { 
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
