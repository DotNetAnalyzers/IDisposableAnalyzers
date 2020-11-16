// ReSharper disable All
namespace ValidCode.NetCore
{
    using Microsoft.AspNetCore.Http;

    public class UsingHttpResponse
    {
        public void M(HttpResponse response)
        {
            response.RegisterForDispose(new Disposable());
            response.RegisterForDisposeAsync(new AsyncDisposable());
        }
    }
}
