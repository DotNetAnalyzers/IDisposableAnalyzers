namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
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
    public class C
    {
        public void M()
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
    public class C
    {
        public object M()
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
    public class C
    {
        public object M()
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
    public class C
    {
        public bool M()
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
    public class C
    {
        public void M()
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
    public class C
    {
        public void M()
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
    public class C
    {
        public void M()
        {
            Id(new C());
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

    public class C
    {
        public Stream M()
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

    public class C
    {
        public Stream M()
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

    public class C
    {
        public Stream M() => File.OpenRead(string.Empty);
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

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
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
        public C M()
        {
            return new C(new Disposable());
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [TestCase("new C()")]
        [TestCase("new C(new Disposable())")]
        [TestCase("new C(new Disposable(), new Disposable())")]
        public void ReturningNewAssigningAndDisposingParams(string objectCreation)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable[] disposables;

        public C(params IDisposable[] disposables)
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
        public C M()
        {
            return new C(new Disposable(), new Disposable());
        }
    }
}".AssertReplace("new C(new Disposable(), new Disposable())", objectCreation);

            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public void ReturningCreateNewAssigningAndDisposing()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
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
        public C M()
        {
            return Create(new Disposable());
        }

        private static C Create(IDisposable disposable) => new C(disposable);
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
        public StreamReader M()
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

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(int value, IDisposable disposable)
            : this(disposable)
        {
        }

        private C(IDisposable disposable)
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
        public C M()
        {
            return new C(1, new Disposable());
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

    public class C
    {
        public StreamReader M()
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

    public class C
    {
        public StreamReader M()
        {
            var reader = new StreamReader(File.OpenRead(string.Empty));
            return reader;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("new CompositeDisposable(File.OpenRead(fileName))")]
        [TestCase("new CompositeDisposable(File.OpenRead(fileName), File.OpenRead(fileName))")]
        [TestCase("new CompositeDisposable { File.OpenRead(fileName) }")]
        [TestCase("new CompositeDisposable { File.OpenRead(fileName), File.OpenRead(fileName) }")]
        [TestCase("new CompositeDisposable(File.OpenRead(fileName), File.OpenRead(fileName)) { File.OpenRead(fileName), File.OpenRead(fileName) }")]
        public void ReturnedInCompositeDisposable(string expression)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public class C
    {
        public static IDisposable M(string fileName) => new CompositeDisposable(File.OpenRead(fileName));
    }
}".AssertReplace("new CompositeDisposable(File.OpenRead(fileName))", expression);
            AnalyzerAssert.Valid(Analyzer, code);
        }
    }
}
