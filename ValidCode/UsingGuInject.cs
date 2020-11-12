// ReSharper disable All
namespace ValidCode
{
    using System;
    using Gu.Inject;

    public class UsingGuInject
    {
        public static void M()
        {
            using var kernel = new Kernel();
            kernel.Bind<IDisposable, Disposable>();
            var disposable1 = kernel.Get<Disposable>();
            var disposable2 = kernel.Get<IDisposable>();
            var disposable3 = (IDisposable)kernel.Get(typeof(Disposable));
            var o = kernel.Get(typeof(Disposable));
        }
    }
}
