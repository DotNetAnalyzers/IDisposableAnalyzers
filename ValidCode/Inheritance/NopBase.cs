namespace ValidCode.Inheritance
{
    using System;

    public class NopBase : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
