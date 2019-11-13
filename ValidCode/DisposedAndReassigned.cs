// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.IO;
    using System.Threading;

    public abstract class DisposedAndReassigned : IDisposable
    {
        private bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

        public static IDisposable DisposedAndReassignedThenReturned(string fileName)
        {
            var x = File.OpenRead(fileName);
            x.Dispose();
            x = File.OpenRead(fileName);
            return x;
        }

        public void DisposeAssignDisposeAssignNull()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        public void DisposeAssignDisposeAssign()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void TryFinally()
        {
            try
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            b = stream.ReadByte();
            stream.Dispose();
        }

        public void Out()
        {
            Stream stream;
            Create(out stream);
            var b = stream.ReadByte();
            stream.Dispose();
            Create(out stream);
            b = stream.ReadByte();
            stream.Dispose();
        }

        public void VarOut()
        {
            Create(out Stream stream);
            var b = stream.ReadByte();
            stream.Dispose();
            Create(out stream);
            b = stream.ReadByte();
            stream.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}
