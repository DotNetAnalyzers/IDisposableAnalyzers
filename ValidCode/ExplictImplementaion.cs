namespace ValidCode
{
    using System;

    public sealed class ExplictImplementaion : IDisposable
    {
        private readonly Disposable disposable = new Disposable();

        void IDisposable.Dispose()
        {
            this.disposable.Dispose();
        }
    }
}
