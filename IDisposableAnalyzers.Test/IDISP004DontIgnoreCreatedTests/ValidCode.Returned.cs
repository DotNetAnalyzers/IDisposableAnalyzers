namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    internal partial class ValidCode<T>
    {
        [Test]
        public void Generic()
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
            AnalyzerAssert.Valid(Analyzer, factoryCode, testCode);
        }

        [Test]
        public void Operator()
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
            AnalyzerAssert.Valid(Analyzer, mehCode, testCode);
        }

        [Test]
        public void OperatorNestedCall()
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
            AnalyzerAssert.Valid(Analyzer, mehCode, testCode);
        }

        [Test]
        public void OperatorEquals()
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
            AnalyzerAssert.Valid(Analyzer, mehCode, testCode);
        }

        [Test]
        public void MethodReturningObject()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodWithArgReturningObject()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodWithObjArgReturningObject()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningStatementBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningLocalStatementBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningExpressionBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningNewAssigningAndDisposing()
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public void ReturningNewAssigningAndDisposingParams()
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public void ReturningCreateNewAssigningAndDisposing()
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public void ReturningCreateNewStreamReader()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningAssigningPrivateChained()
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public void StreamInStreamReader()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StreamInStreamReaderLocal()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
