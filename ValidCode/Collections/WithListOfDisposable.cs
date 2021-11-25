// ReSharper disable All
namespace ValidCode.Collections
{
    using System;
    using System.Collections.Generic;

    internal sealed class WithListOfDisposable : IDisposable
    {
        private readonly List<IDisposable> disposables = new() { new Disposable(), };

        public WithListOfDisposable()
        {
            this.disposables.Add(new Disposable());
        }

        public void Dispose()
        {
            foreach (var disposable in this.disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
