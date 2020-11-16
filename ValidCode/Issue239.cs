// ReSharper disable All
namespace ValidCode
{
    using System;

    public class Issue239
    {
        IDisposable M1()
        {
            IDisposable value = new Foo();

            if (DoStuff())
            {
                value.Dispose();
                value = new Bar();
            }

            return value;
        }

        IDisposable M2()
        {
            IDisposable value = new Foo();
            value.Dispose();
            value = new Bar();
            return value;
        }

        bool DoStuff() => true;

        sealed class Foo : IDisposable
        {
            public void Dispose()
            {
            }
        }

        sealed class Bar : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
