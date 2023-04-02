namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests;

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
            this.Disposable = disposable ?? disposable ?? throw new Exception();
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
        private readonly int n;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
            : this(disposable, 1)
        {
        }

        private C(IDisposable disposable, int n)
        {
            this.n = n;
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
        var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        protected BaseClass(IDisposable disposable)
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

    public sealed class C : BaseClass
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
        RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, code);
    }

    [Test]
    public static void ChainedBaseCtorDisposedInThis()
    {
        var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private readonly object disposable;
        private bool disposed;

        protected BaseClass(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public object P
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

    public sealed class C : BaseClass
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
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, code);
    }

    [TestCase("new StreamReader(File.OpenRead(fileName))")]
    [TestCase("new StreamReader(File.OpenRead(fileName), new System.Text.UTF8Encoding(), true, 1024, leaveOpen: false)")]
    [TestCase("new System.Net.Mail.Attachment(File.OpenRead(fileName), string.Empty)")]
    public static void LeaveOpenFalse(string expression)
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M(string fileName)
        {
            using var reader = new StreamReader(File.OpenRead(fileName));
        }
    }
}".AssertReplace("new StreamReader(File.OpenRead(fileName))", expression);
        RoslynAssert.Valid(Analyzer, code);
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
        public string? M()
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
        public string? M()
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
        public string? M()
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
        public void M1()
        {
            this.M2(() => Task.Delay(0));
        }
        public void M2(Func<Task> func)
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
        public void M1()
        {
            this.M2(() => File.OpenRead(string.Empty));
        }

        public void M2(Func<Stream> func)
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
        public static IKernel Kernel { get; } = new Kernel(
            new NinjectSettings(),
            new Module());
    }

    public class Kernel : StandardKernel
    {
        public Kernel(INinjectSettings settings, params INinjectModule[] modules)
            : base(settings, modules)
        {
        }
    }

    public class Module : NinjectModule
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

        var pairOfT = @"
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

        RoslynAssert.Valid(Analyzer, pairOfT, staticPairCode, code);
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

        var pairOfT = @"
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

        RoslynAssert.Valid(Analyzer, pairOfT, staticPairCode, code);
    }

    [Test]
    public static void PooledMemoryStream()
    {
        var code = @"
namespace N
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    internal class PooledMemoryStream : Stream
    {
        private static readonly ConcurrentQueue<MemoryStream> Pool = new ConcurrentQueue<MemoryStream>();
        private readonly MemoryStream inner;

        private bool disposed;

        private PooledMemoryStream(MemoryStream inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc/>
        public override bool CanRead => !this.disposed;

        /// <inheritdoc/>
        public override bool CanSeek => !this.disposed;

        /// <inheritdoc/>
        public override bool CanWrite => !this.disposed;

        /// <see cref=""MemoryStream.Length""/>
        public override long Length => this.inner.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => this.inner.Position;
            set => this.inner.Position = value;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            // nop
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => this.inner.Seek(offset, origin);

        /// <inheritdoc/>
        public override void SetLength(long value) => this.inner.SetLength(value);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            this.CheckDisposed();
            return this.inner.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.CheckDisposed();
            this.inner.Write(buffer, offset, count);
        }

        internal static PooledMemoryStream Borrow()
        {
            if (Pool.TryDequeue(out var stream))
            {
                return new PooledMemoryStream(stream);
            }

            return new PooledMemoryStream(new MemoryStream());
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.inner.SetLength(0);
                Pool.Enqueue(this.inner);
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
";
        RoslynAssert.Valid(Analyzer, code);
    }
}
