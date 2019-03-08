#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

        private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        [TestCase("this.stream.Dispose();")]
        [TestCase("this.stream?.Dispose();")]
        [TestCase("Stream.Dispose();")]
        [TestCase("Stream?.Dispose();")]
        [TestCase("this.Stream.Dispose();")]
        [TestCase("this.Stream?.Dispose();")]
        [TestCase("Calculated.Dispose();")]
        [TestCase("Calculated?.Dispose();")]
        [TestCase("this.Calculated.Dispose();")]
        [TestCase("this.Calculated?.Dispose();")]
        public void DisposingField(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Stream Calculated => this.stream;

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}".AssertReplace("this.stream.Dispose();", disposeCall);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldInVirtualDispose()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stream.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldInVirtualDispose2()
        {
            var disposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable _disposable = new Disposable();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }
        }

        protected void VerifyDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, disposableCode, testCode);
        }

        [Test]
        public void DisposingFieldInExpressionBodyDispose()
        {
            var disposableCode = @"
namespace RoslynSandbox
{
    using System;
    class Disposable : IDisposable {
        public void Dispose() { }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    class Goof : IDisposable {
        IDisposable _disposable;
        public void Create()  => _disposable = new Disposable();
        public void Dispose() => _disposable.Dispose();
    }
}";
            AnalyzerAssert.Valid(Analyzer, disposableCode, testCode);
        }

        [Test]
        public void DisposingFieldAsCast()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = this.stream as IDisposable;
            disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldInlineAsCast()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.stream as IDisposable)?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldExplicitCast()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = (IDisposable)this.stream;
            disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldInlineExplicitCast()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.stream).Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingPropertyWhenInitializedInProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; private set; }
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingPropertyWhenInitializedInline()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public Stream Stream { get; private set; } = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingPropertyInBaseClass()
        {
            var baseClassCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class CBase : IDisposable
    {
        public abstract Stream Stream { get; }
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : CBase
    {
        public override Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

            AnalyzerAssert.Valid(Analyzer, baseClassCode, testCode);
        }

        [Test]
        public void DisposingPropertyInVirtualDisposeInBaseClass()
        {
            var baseClassCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class CBase : IDisposable
    {
        private bool disposed;

        public abstract Stream Stream { get; }

        /// <inheritdoc/>
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
            if (disposing)
            {
                this.Stream?.Dispose();
            }
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C : CBase
    {
        public override Stream Stream { get; } = File.OpenRead(string.Empty);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, baseClassCode, testCode);
        }

        [TestCase("disposables.First();")]
        [TestCase("disposables.Single();")]
        public void IgnoreLinq(string linq)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq;

    public sealed class C
    {
        private readonly IDisposable _bar;
        
        public C(IDisposable[] disposables)
        {
            _bar = disposables.First();
        }
    }
}".AssertReplace("disposables.First();", linq);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoredWhenNotAssigned()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly IDisposable bar;
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoredWhenBackingField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private Stream stream;

        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoredWhenBackingFieldWithMethodSettingPropertyToNull()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private Stream stream;

        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }

        public void Meh()
        {
            var temp = this.Stream;
            this.Stream = null;
            this.stream = temp;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreFieldThatIsNotDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly object bar = new object();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreFieldThatIsNotDisposableAssignedWithMethod1()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly object bar = Meh();

        private static object Meh() => new object();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreFieldThatIsNotDisposableAssignedWIthMethod2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly object bar = string.Copy(string.Empty);
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoredStaticField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private static Stream stream = File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    public sealed class C
    {
        private readonly Task stream = Task.Delay(0);
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreTaskOfInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    public sealed class C
    {
        private readonly Task<int> stream = Task.FromResult(0);
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FieldOfTypeArrayOfInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        private readonly int[] ints = new[] { 1, 2, 3 };
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PropertyWithBackingFieldOfTypeArrayOfInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int[] ints;

        public int[] Ints
        {
            get
            {
                return this.ints ?? (this.ints = new int[] { });
            }

            set
            {
                this.ints = value;
            }
        }

        public bool HasInts => (this.ints != null) && (this.ints.Length > 0);
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void HandlesRecursion()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        private readonly IDisposable foo = Forever();

        private static IDisposable Forever()
        {
            return Forever();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedListOfInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class C
    {
        private readonly List<int> ints;

        public C(List<int> ints)
        {
            this.ints = ints;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedListOfT()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class C<T>
    {
        private readonly List<T> values;

        public C(List<T> values)
        {
            this.values = values;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingPropertyInBase()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        public virtual Stream Stream { get; } = File.OpenRead(string.Empty);
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Stream.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";

            var barCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class M : C
    {
        public override Stream Stream { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public void WhenCallingBaseDispose()
        {
            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class CBase : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
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
            if (disposing)
            {
                this.disposable.Dispose();
            }
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    public class C : CBase
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooBaseCode, testCode);
        }

        [Test]
        public void DisposingFieldInTearDown()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        private Disposable disposable;

        [SetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [TearDown]
        public void TearDown()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void DisposingFieldInOneTimeTearDown()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        private Disposable disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void Issue150()
        {
            var testCode = @"
namespace ValidCode
{
    using System.Collections.Generic;
    using System.IO;

    public class Issue150
    {
        public Issue150(string name)
        {
            this.Name = name;
            if (File.Exists(name))
            {
                this.AllText = File.ReadAllText(name);
                this.AllLines = File.ReadAllLines(name);
            }
        }

        public string Name { get; }

        public bool Exists => File.Exists(this.Name);

        public string AllText { get; }

        public IReadOnlyList<string> AllLines { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void Tuple(string expression)
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
        public void ValueTuple(string expression)
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
        public void Pair(string expression)
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
