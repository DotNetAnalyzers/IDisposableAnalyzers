namespace ValidCode.Web;

using System;
using Microsoft.Extensions.DependencyInjection;

public static class Issue293
{
    public static void M1(IServiceProvider serviceProvider)
    {
        var disposable1 = serviceProvider.GetService<Disposable>();
        var disposable2 = serviceProvider.GetRequiredService<Disposable>();
    }
}
