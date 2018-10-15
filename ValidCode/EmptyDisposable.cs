namespace ValidCode
{
    using System;

    public class EmptyDisposable
    {
        public IDisposable M(bool b) => b ? new Disposable() : Empty.Default;

        private sealed class Empty : IDisposable
        {
            public static readonly IDisposable Default = new Empty();

            public void Dispose()
            {
            }
        }
    }
}
