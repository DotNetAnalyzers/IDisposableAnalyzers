namespace ValidCode.NetCore.AsyncDisposable
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Issue222 : IAsyncDisposable
    {
        private readonly Stream disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
        }
    }
}
