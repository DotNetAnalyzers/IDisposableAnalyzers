namespace ValidCode.Constructors;

using System;

public sealed class Two : IDisposable
{
    private readonly IDisposable disposable;
    private bool disposed;

    public Two(int _)
    {
        this.disposable = new Disposable();
    }

    public Two(string _)
    {
        this.disposable = new Disposable();
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.disposable?.Dispose();
    }
}
