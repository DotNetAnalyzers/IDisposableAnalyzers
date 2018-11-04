namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class WhenDisposing
        {
            [TestCase("this.stream.Dispose();")]
            [TestCase("this.stream?.Dispose();")]
            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public void DisposingField(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
                testCode = testCode.AssertReplace("this.stream.Dispose();", disposeCall);
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
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void DisposingFieldInExpressionBodyDispose()
            {
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
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
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

        public Stream Stream { get; }
        
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
            public void IgnorePassedInViaCtor1()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;
        
        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtor2()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtor3()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }

        public void Dispose()
        {
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
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
}";
                testCode = testCode.AssertReplace("disposables.First();", linq);
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
        }
    }
}
