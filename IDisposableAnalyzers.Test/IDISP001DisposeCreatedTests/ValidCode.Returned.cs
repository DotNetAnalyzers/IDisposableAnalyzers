namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void SimpleStatementBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SimpleExpressionBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnedTernary()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnedNullConditional()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalFileOpenRead()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalFileOpenReadDisposable()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalFileOpenReadAsDisposable()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalFileOpenReadCastDisposable()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalFileOpenReadAfterAccessingLength()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInIfAndEnd()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInIf()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInStreamReaderMethodBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInLocalStreamReader()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInStreamReaderMethodBodyAsDisposable()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FileOpenReadIsReturnedInCompositeDisposableMethodBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDisposableIsReturnedPropertySimple()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDisposableIsReturnedPropertyBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDisposableIsReturnedPropertyExpressionBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInLazy()
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void LocalFunctionStatementBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalFunctionExpressionBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalInLocalFunction()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
