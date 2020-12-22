// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Reactive.Linq;

    sealed class Issue221 : IDisposable
    {
        private readonly IDisposable subscription;
        private bool disposed;

        public Issue221(IObservable<object> observable = null)
        {
            this.subscription = observable?.Subscribe(_ => { });
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.subscription?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
