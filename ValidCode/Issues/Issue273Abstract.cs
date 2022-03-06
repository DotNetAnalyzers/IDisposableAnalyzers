namespace ValidCode
{
    using System;

    public abstract class Issue273Abstract : IDisposable
    {
        protected Issue273Abstract(int x)
        {
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
