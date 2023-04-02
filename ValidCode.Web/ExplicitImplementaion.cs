// ReSharper disable All
namespace ValidCode.NetCore;

using System;
using ValidCode.Web;

public sealed class ExplicitImplementaion : IDisposable
{
    private Disposable? disposable = new Disposable();

    void IDisposable.Dispose()
    {
        this.disposable?.Dispose();
        this.disposable = null;
    }
}
