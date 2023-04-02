namespace ValidCode;

using System;

public sealed class ExplictImplementaion : IDisposable
{
    private readonly Disposable disposable = new();

    void IDisposable.Dispose()
    {
        this.disposable.Dispose();
    }
}
