namespace ValidCode
{
    using System;

    public sealed class Lazy : IDisposable
    {
        private readonly IDisposable created;
        private bool disposed;
        private IDisposable lazyDisposable;

        public Lazy(IDisposable injected)
        {
            this.Disposable = injected ?? (this.created = new Disposable());
        }

        public IDisposable Disposable { get; }

        public IDisposable LazyDisposable => this.lazyDisposable ?? (this.lazyDisposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.created?.Dispose();
            this.lazyDisposable?.Dispose();
        }
    }
}
