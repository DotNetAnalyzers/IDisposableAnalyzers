namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
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

        private const string ExplicitDisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class ExplicitDisposable : IDisposable
    {
        void IDisposable.Dispose()
        {
        }
    }
}";

        [Test]
        public void NotDisposingVariable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            // keeping it safe and doing ?.Dispose()
            // will require some work to figure out if it can be null
            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingVariableOfTypeObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            (stream as System.IDisposable)?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AssigningParameterTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AssigningInIfElse()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            if (true)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                ↓stream = File.OpenRead(string.Empty);
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            if (true)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingInitializedFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingInitializedPropertyInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            ↓this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            this.Stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingInitializedBackingFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            ↓this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingBackingFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
            ↓this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
            this.stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) => ↓this.stream = File.OpenRead(string.Empty);
        }

        public event EventHandler Bar;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) =>
            {
                this.stream?.Dispose();
                this.stream = File.OpenRead(string.Empty);
            };
        }

        public event EventHandler Bar;
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnMethodStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh()
        {
            return ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh()
        {
            this.stream?.Dispose();
            return this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnMethodExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh() => ↓this.stream = File.OpenRead(string.Empty);
    }
}";

            ////            var fixedCode = @"
            ////using System;
            ////using System.IO;

            ////public class Foo
            ////{
            ////    private Stream stream;

            ////    public IDisposable Meh()
            ////    {
            ////        this.stream?.Dispose();
            ////        return this.stream = File.OpenRead(string.Empty);
            ////    }
            ////}";
            // Not implementing the fix for now, not a common case.
            AnalyzerAssert.NoFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnStatementInPropertyStamementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh
        {
            get
            {
                return ↓this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh
        {
            get
            {
                this.stream?.Dispose();
                return this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnStatementInPropertyExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh => ↓this.stream = File.OpenRead(string.Empty);
    }
}";

            ////            var fixedCode = @"
            ////namespace RoslynSandbox
            ////{
            ////    using System;
            ////    using System.IO;

            ////    public class Foo
            ////    {
            ////        private Stream stream;

            ////        public IDisposable Meh
            ////        {
            ////            get
            ////            {
            ////                this.stream?.Dispose();
            ////                return this.stream = File.OpenRead(string.Empty);
            ////            }
            ////        }
            ////    }
            ////}";
            // Not implementing the fix for now, not a common case.
            AnalyzerAssert.NoFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode);
        }

        [Test]
        public void AssigningFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                ↓this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void AssigningBackingFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                ↓this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
            private set { this.disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
            private set { this.disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }
    }
}
