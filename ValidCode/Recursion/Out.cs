// ReSharper disable All
namespace ValidCode.Recursion
{
    using System;

    public sealed class Out : IDisposable
    {
        public Out()
        {
            StatementBody(out var value);
            StatementBody(1, out value);
            StatementBody(1.0, out value);
            StatementBody(string.Empty, out value);
        }

        public static bool StatementBody(out IDisposable value)
        {
            return StatementBody(out value);
        }

        public static bool StatementBody(int foo, out IDisposable value)
        {
            return StatementBody(2, out value);
        }

        public static bool StatementBody(double foo, out IDisposable value)
        {
            value = null;
            return StatementBody(3.0, out value);
        }

        public static bool StatementBody(string foo, out IDisposable value)
        {
            if (foo == null)
            {
                return StatementBody(3.0, out value);
            }

            value = null;
            return true;
        }

        public void DispoeAndReassign()
        {
            StatementBody(out var value);
            value.Dispose();
            StatementBody(1, out value);
            value.Dispose();
            StatementBody(1.0, out value);
            value.Dispose();
            StatementBody(string.Empty, out value);
            value.Dispose();
        }

        public void Dispose()
        {
            StatementBody(out var value);
            value.Dispose();
            StatementBody(1, out value);
            value.Dispose();
            StatementBody(1.0, out value);
            value.Dispose();
            StatementBody(string.Empty, out value);
            value.Dispose();
        }

        public void SameAsCtor()
        {
            StatementBody(out var value);
            StatementBody(1, out value);
            StatementBody(1.0, out value);
            StatementBody(string.Empty, out value);
        }
    }
}
