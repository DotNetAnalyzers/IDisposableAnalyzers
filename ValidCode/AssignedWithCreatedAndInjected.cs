#pragma warning disable IDISP008
namespace ValidCode
{
    using System;

    public class AssignedWithCreatedAndInjected
    {
        private readonly IDisposable disposable;

        public AssignedWithCreatedAndInjected()
        {
            this.disposable = new Disposable();
        }

        public AssignedWithCreatedAndInjected(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}
