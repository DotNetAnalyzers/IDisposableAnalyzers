// ReSharper disable All
namespace ValidCode.NetCore
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class ServiceProviderUsage
    {
        private readonly IServiceProvider serviceProvider;

        public ServiceProviderUsage(IServiceProvider serviceProvider)
        {
            var disposable1 = serviceProvider.GetRequiredService<Disposable>();
            _ = serviceProvider.GetRequiredService<Disposable>();
            var loggerFactory1 = serviceProvider.GetRequiredService<ILoggerFactory>();
            _ = serviceProvider.GetRequiredService<ILoggerFactory>();

            this.serviceProvider = serviceProvider;
            var disposable2 = this.serviceProvider.GetRequiredService<Disposable>();
            _ = this.serviceProvider.GetRequiredService<Disposable>();
            var loggerFactory2 = this.serviceProvider.GetRequiredService<ILoggerFactory>();
            _ = this.serviceProvider.GetRequiredService<ILoggerFactory>();
        }

        public sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
