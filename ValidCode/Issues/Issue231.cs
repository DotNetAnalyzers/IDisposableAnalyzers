namespace ValidCode;

using System;
using Microsoft.Extensions.DependencyInjection;

public static class Issue231
{
    public static ServiceCollection M1(ServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped(x => new Disposable());
        serviceCollection.AddSingleton(typeof(IDisposable), x => new Disposable());
        return serviceCollection;
    }

    public static IServiceCollection M2(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped(x => new Disposable());
        serviceCollection.AddSingleton(typeof(IDisposable), x => new Disposable());
        return serviceCollection;
    }
}
