// ReSharper disable All
namespace ValidCode
{
    using System;

    public sealed class Issue273 : IDisposable
    {
        private readonly Disposable disposable;

        public Issue273(int x, int y)
            : this(x + y)
        {
        }

        public Issue273(int x)
        {
            this.disposable = new Disposable();
        }

        public Issue273(string x)
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}
