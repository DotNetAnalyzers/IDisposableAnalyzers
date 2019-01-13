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

    public sealed class Foo : IDisposable
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

    public class Foo : IDisposable
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

    public class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
    {
        public Foo()
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

    public sealed class Foo : IDisposable
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

    public abstract class FooBase : IDisposable
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

    public sealed class Foo : FooBase
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

    public abstract class FooBase : IDisposable
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

    public sealed class Foo : FooBase
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

    public sealed class Foo
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable[] disposables)
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

    public sealed class Foo
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

    public sealed class Foo
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

    public sealed class Foo
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
    public class Foo
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
    public class Foo
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
    public class Foo
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

    public sealed class Foo
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

    public sealed class Foo
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

    public sealed class Foo
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
    public sealed class Foo
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
    public sealed class Foo
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

    public class Foo
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

    public class Foo
    {
        private readonly List<int> ints;

        public Foo(List<int> ints)
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

    public class Foo<T>
    {
        private readonly List<T> values;

        public Foo(List<T> values)
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

    public class Foo : IDisposable
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

    public class Bar : Foo
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

    public abstract class FooBase : IDisposable
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
    public class Foo : FooBase
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
    }
}
