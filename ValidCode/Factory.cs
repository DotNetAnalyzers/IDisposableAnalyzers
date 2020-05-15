// ReSharper disable All
namespace ValidCode
{
    using System;

    public class Factory
    {
        public IDisposable Create() => new Disposable();
    }
}
