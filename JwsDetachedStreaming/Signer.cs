using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JwsDetachedStreaming
{
    public abstract partial class Signer
    {
        public abstract ValueTask WriteInputAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

        public abstract Task<byte[]> GetSignatureAsync(CancellationToken cancellationToken = default);
    }

    public abstract partial class Signer : Stream
    {
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
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

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return WriteInputAsync(buffer, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
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
