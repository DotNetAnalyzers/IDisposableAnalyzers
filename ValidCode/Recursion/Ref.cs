// ReSharper disable All
namespace ValidCode.Recursion
{
    using System;

    public sealed class Ref : IDisposable
    {
        public Ref()
        {
            IDisposable value = null;
            RefStatementBody(ref value);
        }

        public static bool RefStatementBody(ref IDisposable value)
        {
            return RefStatementBody(ref value);
        }

        public void Dispose()
        {
            IDisposable value = null;
            RefStatementBody(ref value);
            value.Dispose();
        }

        public void SameAsCtor()
        {
            IDisposable value = null;
            RefStatementBody(ref value);
        }
    }
}
