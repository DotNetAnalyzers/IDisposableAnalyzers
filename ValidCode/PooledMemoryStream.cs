namespace ValidCode
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    internal class PooledMemoryStream : Stream
    {
        private static readonly ConcurrentQueue<MemoryStream> Pool = new();
        private readonly MemoryStream inner;

        private bool disposed;

        private PooledMemoryStream(MemoryStream inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc/>
        public override bool CanRead => !this.disposed;

        /// <inheritdoc/>
        public override bool CanSeek => !this.disposed;

        /// <inheritdoc/>
        public override bool CanWrite => !this.disposed;

        /// <see cref="MemoryStream.Length"/>
        public override long Length => this.inner.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => this.inner.Position;
            set => this.inner.Position = value;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            // nop
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => this.inner.Seek(offset, origin);

        /// <inheritdoc/>
        public override void SetLength(long value) => this.inner.SetLength(value);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            this.CheckDisposed();
            return this.inner.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.CheckDisposed();
            this.inner.Write(buffer, offset, count);
        }

        internal static PooledMemoryStream Borrow()
        {
            if (Pool.TryDequeue(out var stream))
            {
                return new PooledMemoryStream(stream);
            }

            return new PooledMemoryStream(new MemoryStream());
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.inner.SetLength(0);
                Pool.Enqueue(this.inner);
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
