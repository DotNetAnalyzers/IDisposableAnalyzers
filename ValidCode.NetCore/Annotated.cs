namespace ValidCode.NetCore
{
    using System;

    sealed class Annotated : IDisposable
    {
        private IDisposable? _foo;
        private bool _disposed;

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            _foo?.Dispose();
        }

        void Reassign()
        {
            _foo!.Dispose();
            _foo = new Disposable();
        }
    }
}
