namespace ValidCode.Web.AsyncDisposableCases;

using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class Issue199 : IAsyncDisposable
{
    private Timer? _timer;

    public async Task ResetTimerAsync()
    {
        if (this._timer != null)
        {
            await this._timer.DisposeAsync();
            this._timer = null; // Warns with IDISP003: Dispose previous before re-assigning
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this._timer is { })
        {
            await this._timer.DisposeAsync().ConfigureAwait(false);
        }
    }
}
