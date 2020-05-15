namespace ValidCode.Recursion.Nested
{
    using AliasedDisposable = AliasedDisposable;

    public class Alias
    {
        public Alias()
        {
            using (new AliasedDisposable())
            {
            }
        }
    }
}

namespace ValidCode.Recursion
{
    using System;

    public sealed class AliasedDisposable : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
