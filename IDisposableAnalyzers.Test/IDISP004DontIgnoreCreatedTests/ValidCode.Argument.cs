namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        [Test]
        public void ChainedCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void ChainedCtorCoalesce()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
        {
            this.Disposable = disposable ?? disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void ChainedCtors()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly int meh;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
            : this(disposable, 1)
        {
        }

        private C(IDisposable disposable, int meh)
        {
            this.meh = meh;
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void ChainedCtorCallsBaseCtorDisposedInThis()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class CBase : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        protected CBase(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable Disposable => this.disposable;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : CBase
    {
        private bool disposed;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
            : base(disposable)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.Disposable.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, baseCode, testCode);
        }

        [Test]
        public void ChainedBaseCtorDisposedInThis()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class CBase : IDisposable
    {
        private readonly object disposable;
        private bool disposed;

        protected CBase(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public object M
        {
            get
            {
                return this.disposable;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : CBase
    {
        private bool disposed;

        public C()
            : base(new Disposable())
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                (this.M as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, baseCode, testCode);
        }

        [Test]
        public void UsingStreamInStreamReader()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public string M()
        {
            using (var reader = new StreamReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposableCreate()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (System.Reactive.Disposables.Disposable.Create(() => File.OpenRead(string.Empty)))
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningStreamReader()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public string M()
        {
            using (var reader = GetReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }

        private static StreamReader GetReader(Stream stream)
        {
            return new StreamReader(stream);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningStreamReaderExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public string M()
        {
            using (var reader = GetReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }

        private static StreamReader GetReader(Stream stream) => new StreamReader(stream);
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodWithFuncTaskAsParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;
    public class C
    {
        public void Meh()
        {
            this.M(() => Task.Delay(0));
        }
        public void M(Func<Task> func)
        {
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodWithFuncStreamAsParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void Meh()
        {
            this.M(() => File.OpenRead(string.Empty));
        }

        public void M(Func<Stream> func)
        {
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SubclassedNinjectKernel()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using Ninject;
    using Ninject.Modules;

    public static class C
    {
        public static IKernel Kernel { get; } = new CKernel(
            new NinjectSettings(),
            new CModule());
    }

    public class CKernel : StandardKernel
    {
        public CKernel(INinjectSettings settings, params INinjectModule[] modules)
            : base(settings, modules)
        {
        }
    }

    public class CModule : NinjectModule
    {
        public override void Load()
        {
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ValueTupleOfLocals()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.tuple = (stream1, stream2);
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ValueTuple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            this.tuple = (File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TupleOfLocals()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.tuple = Tuple.Create(stream1, stream2);
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalObjectThatIsDisposed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string fileName)
        {
            object o = File.OpenRead(fileName);
            ((IDisposable)o).Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FieldObjectThatIsDisposed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object o;

        public C(string fileName)
        {
            this.o = File.OpenRead(fileName);
        }

        public void Dispose()
        {
            ((IDisposable)this.o).Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file), new object())")]
        [TestCase("Tuple.Create(File.OpenRead(file), File.OpenRead(file))")]
        [TestCase("new Tuple<FileStream, object>(File.OpenRead(file), new object())")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file), File.OpenRead(file))")]
        public void LocalTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = Tuple.Create(File.OpenRead(file), new object());
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file), new object())", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file), new object())")]
        [TestCase("(File.OpenRead(file), File.OpenRead(file))")]
        public void LocalValueTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = (File.OpenRead(file), new object());
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file), new object())", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void LocalPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace RoslynSandbox
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace RoslynSandbox
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file1, string file2)
        {
            var pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
            pair.Item1.Dispose();
            pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, genericPairCode, staticPairCode, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            this.tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldValueTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            this.tuple = (File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace RoslynSandbox
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace RoslynSandbox
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public C(string file1, string file2)
        {
            this.pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.pair.Item1.Dispose();
            this.pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, genericPairCode, staticPairCode, testCode);
        }
    }
}
