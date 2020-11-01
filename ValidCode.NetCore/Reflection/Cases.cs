namespace ValidCode.NetCore.Reflection
{
    using System;
    using System.Text;

    using Microsoft.Extensions.DependencyInjection;

    public class Cases
    {
        public static void ActivatorCreateInstanceOfDisposable(IServiceProvider provider)
        {
            using var disposable = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Disposable>(provider);
            var builder = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<StringBuilder>(provider);
        }
    }
}
