namespace ValidCode.Inheritance
{
    using System;

    public abstract class AbstractDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
