namespace ValidCode
{
    using System;

    public sealed class Issue273Concrete : Issue273Abstract, IDisposable
    {
        private readonly Disposable disposable;

        public Issue273Concrete(int x)
            : base(x)
        {
            this.disposable = new Disposable();
        }

        public Issue273Concrete(string x)
            : base(x.Length)
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}
