namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [Test]
        public static void SimpleStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void SimpleExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M() => File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturnedTernary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M(string fileName) => fileName == null ? File.OpenRead(string.Empty) : File.OpenRead(fileName);
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturnedNullConditional()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M(string fileName) => File.OpenRead(string.Empty) ?? File.OpenRead(fileName);
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalFileOpenRead()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalFileOpenReadDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class C
    {
        public static IDisposable M()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalFileOpenReadAsDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class C
    {
        public static IDisposable M()
        {
            var stream = File.OpenRead(string.Empty);
            return stream as IDisposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalFileOpenReadCastDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class C
    {
        public static IDisposable M()
        {
            var stream = File.OpenRead(string.Empty);
            return (IDisposable)stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalFileOpenReadAfterAccessingLength()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M()
        {
            var stream = File.OpenRead(string.Empty);
            var length = stream.Length;
            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInIfAndEnd()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M()
        {
            var stream = File.OpenRead(string.Empty);
            if (true)
            {
                return stream;
            }

            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInIf()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M(string text)
        {
            var stream = File.OpenRead(string.Empty);
            if (text == null)
            {
                return stream;
            }

            var length = stream.Length;
            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInStreamReaderMethodBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static StreamReader M()
        {
            var stream = File.OpenRead(string.Empty);
            return new StreamReader(stream);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInLocalStreamReader()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static StreamReader M()
        {
            var stream = File.OpenRead(string.Empty);
            var reader = new StreamReader(stream);
            return reader;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInStreamReaderMethodBodyAsDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static StreamReader M()
        {
            var stream = File.OpenRead(string.Empty);
            return new StreamReader(stream);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FileOpenReadIsReturnedInCompositeDisposableMethodBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Reactive.Disposables;

    public static class C
    {
        public static CompositeDisposable M()
        {
            var stream = File.OpenRead(string.Empty);
            return new CompositeDisposable { stream };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenDisposableIsReturnedPropertySimple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M
        {
            get
            {
                return File.OpenRead(string.Empty);;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenDisposableIsReturnedPropertyBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M
        {
            get
            {
                var stream = File.OpenRead(string.Empty);
                return stream;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenDisposableIsReturnedPropertyExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream M => File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInLazy()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly Lazy<IDisposable> disposable;

        public C()
        {
            this.disposable = new Lazy<IDisposable>(() =>
            {
                var temp = new Disposable();
                return temp;
            });
        }

        public void Dispose()
        {
            if (this.disposable.IsValueCreated)
            {
                this.disposable.Value.Dispose();
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, testCode);
        }

        [Test]
        public static void LocalFunctionStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    class C
    {
        public static IDisposable Create()
        {
            return CreateCore();

            IDisposable CreateCore()
            {
                return File.OpenRead(string.Empty);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalFunctionExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    class C
    {
        public static IDisposable Create()
        {
            return CreateCore();

            IDisposable CreateCore() => File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalInLocalFunction()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    class C
    {
        public static IDisposable Create()
        {
            return CreateCore();

            IDisposable CreateCore()
            {
                var disposable = File.OpenRead(string.Empty);
                return disposable;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
