// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Threading;

    sealed class Issue209 : IDisposable
    {
        private IDisposable _disposable = new Disposable();

        public void Dispose()
        {
            var oldValue = Interlocked.Exchange(ref _disposable, null);
            oldValue?.Dispose();
        }
    }
}
