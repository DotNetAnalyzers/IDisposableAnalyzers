// ReSharper disable All
namespace ValidCode.NetCore
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class ServiceProviderUsage
    {
        public ServiceProviderUsage(IServiceProvider serviceProvider)
        {
            var disposable = serviceProvider.GetRequiredService<Disposable>();
            _ = serviceProvider.GetRequiredService<Disposable>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _ = serviceProvider.GetRequiredService<ILoggerFactory>();
        }

        public sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
