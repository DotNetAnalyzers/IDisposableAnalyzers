namespace ValidCode.Recursion
{
    using System;

    public class RefAndOut : IDisposable
    {
        public RefAndOut()
        {
            RecursiveOut(out var value);
            RecursiveOut(1, out value);
            RecursiveOut(1.0, out value);
            RecursiveOut(string.Empty, out value);
            RecursiveRef(ref value);
        }

        public static bool RecursiveOut(out IDisposable value)
        {
            return RecursiveOut(out value);
        }

        public static bool RecursiveOut(int foo, out IDisposable value)
        {
            return RecursiveOut(2, out value);
        }

        public static bool RecursiveOut(double foo, out IDisposable value)
        {
            value = null;
            return RecursiveOut(3.0, out value);
        }

        public static bool RecursiveOut(string foo, out IDisposable value)
        {
            if (foo == null)
            {
                return RecursiveOut(3.0, out value);
            }

            value = null;
            return true;
        }

        public static bool RecursiveRef(ref IDisposable value)
        {
            return RecursiveRef(ref value);
        }

        public void Dispose()
        {
#pragma warning disable IDISP016 // Don't use disposed instance.
            RecursiveOut(out var value);
            value.Dispose();
            RecursiveOut(1, out value);
            value.Dispose();
            RecursiveOut(1.0, out value);
            value.Dispose();
            RecursiveOut(string.Empty, out value);
            value.Dispose();
            RecursiveRef(ref value);
            value.Dispose();
#pragma warning enable IDISP016 // Don't use disposed instance.
        }

        public void SameAsCtor()
        {
            RecursiveOut(out var value);
            RecursiveOut(1, out value);
            RecursiveOut(1.0, out value);
            RecursiveOut(string.Empty, out value);
            RecursiveRef(ref value);
        }
    }
}
