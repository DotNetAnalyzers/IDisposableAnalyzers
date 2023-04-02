namespace ValidCode.Partial;

using System;
using System.IO;

public partial class PartialClass : IDisposable
{
    private readonly IDisposable disposable = File.OpenRead(string.Empty);

    private bool disposed;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }
}
