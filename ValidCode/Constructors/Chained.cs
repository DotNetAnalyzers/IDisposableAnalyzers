// ReSharper disable All
namespace RoslynSandbox
{
    using System;

    public class Chained
    {
        private readonly IDisposable disposable;

        public Chained(IDisposable disposable)
             : this(1)
        {
            this.disposable = disposable;
        }

        public Chained(int n)
        {
            this.disposable = null;
        }
    }
}
