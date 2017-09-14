namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposable>
    {
        internal class Returned : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task Generic()
            {
                var factoryCode = @"
namespace RoslynSandbox
{
    public class Factory
    {
        public static T Create<T>() where T : new() => new T();
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Factory.Create<int>();
        }
    }
}";
                await this.VerifyHappyPathAsync(factoryCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task Operator()
            {
                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public object Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 + meh2;
        }
    }
}";
                await this.VerifyHappyPathAsync(mehCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task OperatorNestedCall()
            {
                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public object Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return Add(new Meh(), new Meh());
        }

        public object Add(Meh meh1, Meh meh2)
        {
            return meh1 + meh2;
        }
    }
}";
                await this.VerifyHappyPathAsync(mehCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task OperatorEquals()
            {
                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public bool Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 == meh2;
        }
    }
}";
                await this.VerifyHappyPathAsync(mehCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task MethodReturningObject()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static object Meh() => new object();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task MethodWithArgReturningObject()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Meh(""Meh"");
        }

        private static object Meh(string arg) => new object();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task MethodWithObjArgReturningObject()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Id(new Foo());
        }

        private static object Id(object arg) => arg;
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningStatementBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningLocalStatementBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar() => File.OpenRead(string.Empty);
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningNewAssigningAndDisposing()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(new Disposable());
        }
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningNewAssigningAndDisposingParams()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable[] disposables;

        public Foo(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in this.disposables)
            {
                disposable.Dispose();
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(new Disposable(), new Disposable());
        }
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningCreateNewAssigningAndDisposing()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Meh
    {
        public Foo Bar()
        {
            return Create(new Disposable());
        }

        private static Foo Create(IDisposable disposable) => new Foo(disposable);
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningCreateNewStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Meh
    {
        public StreamReader Bar()
        {
            return Create(File.OpenRead(string.Empty));
        }

        private static StreamReader Create(Stream stream) => new StreamReader(stream);
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningAssigningPrivateChained()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(int value, IDisposable disposable)
            : this(disposable)
        {
        }

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(1, new Disposable());
        }
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task StreamInStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public StreamReader Bar()
        {
            return new StreamReader(File.OpenRead(string.Empty));
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task StreamInStreamReaderLocal()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public StreamReader Bar()
        {
            var reader = new StreamReader(File.OpenRead(string.Empty));
            return reader;
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }
        }
    }
}