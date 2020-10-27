namespace ValidCode.Chained
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable M() => this;

        public void Dispose()
        {
        }
    }
}
