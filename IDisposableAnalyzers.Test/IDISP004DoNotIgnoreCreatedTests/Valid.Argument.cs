namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        [Test]
        public static void ChainedCtor()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void ChainedCtorCoalesce()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void ChainedCtors()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void ChainedCtorCallsBaseCtorDisposedInThis()
        {
            var baseCode = @"
namespace N
{
    using System;

    public class Base : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        protected Base(IDisposable disposable)
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

            var code = @"
namespace N
{
    using System;

    public sealed class C : Base
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
            RoslynAssert.Valid(Analyzer, DisposableCode, baseCode, code);
        }

        [Test]
        public static void ChainedBaseCtorDisposedInThis()
        {
            var baseCode = @"
namespace N
{
    using System;

    public class Base : IDisposable
    {
        private readonly object disposable;
        private bool disposed;

        protected Base(IDisposable disposable)
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

            var code = @"
namespace N
{
    using System;

    public sealed class C : Base
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
            RoslynAssert.Valid(Analyzer, DisposableCode, baseCode, code);
        }

        [Test]
        public static void UsingStreamInStreamReader()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposableCreate()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodReturningStreamReader()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodReturningStreamReaderExpressionBody()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodWithFuncTaskAsParameter()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodWithFuncStreamAsParameter()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SubclassedNinjectKernel()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ValueTupleOfLocals()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ValueTuple()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TupleOfLocals()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalObjectThatIsDisposed()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void FieldObjectThatIsDisposed()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("Tuple.Create(File.OpenRead(file), new object())")]
        [TestCase("Tuple.Create(File.OpenRead(file), File.OpenRead(file))")]
        [TestCase("new Tuple<FileStream, object>(File.OpenRead(file), new object())")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file), File.OpenRead(file))")]
        public static void LocalTupleThatIsDisposed(string expression)
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("(File.OpenRead(file), new object())")]
        [TestCase("(File.OpenRead(file), File.OpenRead(file))")]
        public static void LocalValueTupleThatIsDisposed(string expression)
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void LocalPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace N
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace N
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

            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, genericPairCode, staticPairCode, code);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldTupleThatIsDisposed(string expression)
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldValueTupleThatIsDisposed(string expression)
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace N
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace N
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

            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, genericPairCode, staticPairCode, code);
        }
    }
}
