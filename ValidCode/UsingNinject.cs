// ReSharper disable All
namespace ValidCode;

using System;
using Ninject;

public class UsingNinject
{
    public static void M()
    {
        using var kernel = new StandardKernel();
        var disposable1 = kernel.Get<Disposable>();
        var o1 = kernel.Get(typeof(Disposable));
        var disposabl2 = (IDisposable)kernel.Get(typeof(Disposable));
    }
}
