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
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SimpleExpressionBody()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream M() => File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturnedTernary()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream M(string fileName) => fileName == null ? File.OpenRead(string.Empty) : File.OpenRead(fileName);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturnedNullConditional()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream M(string fileName) => File.OpenRead(string.Empty) ?? File.OpenRead(fileName);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalFileOpenRead()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalFileOpenReadDisposable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalFileOpenReadAsDisposable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalFileOpenReadCastDisposable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalFileOpenReadAfterAccessingLength()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInIfAndEnd()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream M(bool b)
        {
            var stream = File.OpenRead(string.Empty);
            if (b)
            {
                return stream;
            }

            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInIf()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInStreamReaderMethodBody()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInLocalStreamReader()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInStreamReaderMethodBodyAsDisposable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void FileOpenReadIsReturnedInCompositeDisposableMethodBody()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenDisposableIsReturnedPropertySimple()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenDisposableIsReturnedPropertyBody()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenDisposableIsReturnedPropertyExpressionBody()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream P => File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInLazy()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void LocalFunctionStatementBody()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalFunctionExpressionBody()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalInLocalFunction()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturnedInTupleIssue320()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class Issue320
    {
        public (MemoryStream Stream, int N) M()
        {
            var stream = new MemoryStream();
            return (stream, 1);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
