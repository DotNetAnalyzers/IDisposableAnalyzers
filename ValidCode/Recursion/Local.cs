#pragma warning disable 1717
namespace ValidCode.Recursion
{
    using System;

    public class Local
    {
        public Local(IDisposable disposable)
        {
            var value = disposable;
            value = value;
        }
    }
}
