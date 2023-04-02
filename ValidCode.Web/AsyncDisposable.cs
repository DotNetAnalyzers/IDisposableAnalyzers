namespace ValidCode.Web;

using System;
using System.Threading.Tasks;

public sealed class AsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
