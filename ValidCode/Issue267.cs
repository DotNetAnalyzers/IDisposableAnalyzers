namespace ValidCode
{
    using System;

    internal sealed class Issue267 : IDisposable
    {
        private readonly Disposable? disposable;

        internal Issue267()
        {
            this.M(ref this.disposable);
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }

        private void M(ref Disposable? item)
        {
            if (item is { })
            {
                return;
            }

            item = new Disposable();
        }
    }
}
