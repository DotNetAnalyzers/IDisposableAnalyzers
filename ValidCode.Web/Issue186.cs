namespace ValidCode.Web;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class HostedService : IHostedService
{
    private IDisposable? disposable; // Should not trigger IDISP002 nor IDISP006 : Usage is correct

    public Task StartAsync(CancellationToken token)
    {
        this.disposable = new Disposable(); // Should not trigger IDISP003 : not assigned from the constructor, and this method is made to be called only once. No need to dispose previous
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token)
    {
        this.disposable?.Dispose();
        return Task.CompletedTask;
    }
}
