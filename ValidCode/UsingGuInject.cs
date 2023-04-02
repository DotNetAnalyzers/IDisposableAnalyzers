// ReSharper disable All
namespace ValidCode;

using System;
using Gu.Inject;

public static class UsingGuInject
{
    public static void NewkernelBind()
    {
        using var kernel = new Kernel();
        kernel.Bind<IDisposable, Disposable>();
        var disposable1 = kernel.Get<Disposable>();
        var disposable2 = kernel.Get<IDisposable>();
        var disposable3 = (IDisposable?)kernel.Get(typeof(Disposable));
        var o = kernel.Get(typeof(Disposable));
    }

    public static Kernel CreateKernelReturnExpression(bool useFiles = false)
    {
        var kernel = Create()
            .BindDisposable()
            .BindLazy(useFiles)
            .Rebind<IDisposable, Disposable>();
        return kernel;
    }

    public static Kernel CreateKernelReturnStatements(bool useFiles = false)
    {
        var kernel = Create()
                     .BindDisposable()
                     .BindLazy(useFiles);
        kernel.Rebind<IDisposable, Disposable>();
        return kernel;
    }

    public static Kernel M(IDisposable disposable)
    {
        return Create()
            .Rebind(disposable);
    }
    
    private static Kernel BindDisposable(this Kernel container)
    {
        container.Bind<IDisposable, Disposable>();
        return container;
    }

    private static Kernel BindLazy(this Kernel container, bool useFiles = false)
    {
        container.Bind<Lazy>(c => new Lazy(c.Get<IDisposable>()));
        return container;
    }

    private static Kernel Create()
    {
        var container = new Kernel();
        container.Creating += OnCreating;
        container.Created += OnCreated;
        return container;
    }

    private static void OnCreated(object? sender, CreatedEventArgs e)
    {
    }

    private static void OnCreating(object? sender, CreatingEventArgs creatingEventArgs)
    {
    }
}
