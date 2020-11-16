namespace ValidCode.NetCore.AsyncDisposableCases
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class Issue204
    {
        public async ValueTask M()
        {
            var c = new C();
            try
            {

            }
            finally
            {
                await c.DisposeAsync()
                       .ConfigureAwait(false);
            }
        }

        public class C : IAsyncDisposable
        {
            private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

            public async ValueTask DisposeAsync()
            {
                await this.disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
