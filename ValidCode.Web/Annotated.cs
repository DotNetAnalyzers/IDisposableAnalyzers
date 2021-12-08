namespace ValidCode.Web
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
            this._foo?.Dispose();
        }

        void Reassign()
        {
            this._foo!.Dispose();
            this._foo = new Disposable();
        }
    }
}
