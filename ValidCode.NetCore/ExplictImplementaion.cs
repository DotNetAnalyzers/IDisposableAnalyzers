namespace ValidCode.NetCore
{
    using System;

    public sealed class ExplictImplementaion : IDisposable
    {
        private Disposable? disposable = new Disposable();

        void IDisposable.Dispose()
        {
            this.disposable?.Dispose();
            this.disposable = null;
        }
    }
}
