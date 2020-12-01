// ReSharper disable All
namespace ValidCode
{
    using System;
    using Gu.Inject;

    public class UsingGuInject
    {
        public static void NewkernelBind()
        {
            using var kernel = new Kernel();
            kernel.Bind<IDisposable, Disposable>();
            var disposable1 = kernel.Get<Disposable>();
            var disposable2 = kernel.Get<IDisposable>();
            var disposable3 = (IDisposable)kernel.Get(typeof(Disposable));
            var o = kernel.Get(typeof(Disposable));
        }

        public static Kernel CreateKernelReturn()
        {
            var kernel = Create()
                .Bind<IDisposable, Disposable>();
            return kernel;
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            container.Creating += OnResolving;
            container.Created += OnResolved;
            return container;
        }

        private static void OnResolved(object sender, object e)
        {
        }

        private static void OnResolving(object sender, Type e)
        {
        }
    }
}
