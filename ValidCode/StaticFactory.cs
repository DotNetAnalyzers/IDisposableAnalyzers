// ReSharper disable All
namespace ValidCode
{
    using System;

    public static class StaticFactory
    {
        public static IDisposable Create() => new Disposable();
    }
}
