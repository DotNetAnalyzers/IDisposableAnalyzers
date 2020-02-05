namespace ValidCode.NetCore.AsyncDisposable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class Issue199 : IAsyncDisposable
    {
        private Timer? _timer;

        public async Task ResetTimerAsync()
        {
            if (_timer != null)
            {
                await _timer.DisposeAsync();
                _timer = null; // Warns with IDISP003: Dispose previous before re-assigning.
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this._timer is { })
            {
                await _timer.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
