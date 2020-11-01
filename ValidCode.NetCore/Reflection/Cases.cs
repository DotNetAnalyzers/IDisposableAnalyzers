namespace ValidCode.NetCore.Reflection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class Cases
    {
        public static void ActivatorCreateInstanceOfDisposable(IServiceProvider provider)
        {
            using var disposable = ActivatorUtilities.CreateInstance<Disposable>(provider);
        }
    }
}
