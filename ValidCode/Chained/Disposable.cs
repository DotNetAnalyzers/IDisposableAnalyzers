namespace ValidCode.Chained
{
    using System;

    public sealed class Disposable : IDisposable
    {
        public Disposable M() => this;

        public void Dispose()
        {
        }
    }
}
