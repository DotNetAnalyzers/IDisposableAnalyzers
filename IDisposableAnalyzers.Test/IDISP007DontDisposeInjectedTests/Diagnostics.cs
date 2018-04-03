namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly IDISP007DontDisposeInjected Analyzer = new IDISP007DontDisposeInjected();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP007");

        [TestCase("stream ?? File.OpenRead(string.Empty)")]
        [TestCase("Stream ?? File.OpenRead(string.Empty)")]
        [TestCase("File.OpenRead(string.Empty) ?? stream")]
        [TestCase("File.OpenRead(string.Empty) ?? Stream")]
        [TestCase("true ? stream : File.OpenRead(string.Empty)")]
        [TestCase("true ? Stream : File.OpenRead(string.Empty)")]
        [TestCase("true ? File.OpenRead(string.Empty) : stream")]
        [TestCase("true ? File.OpenRead(string.Empty) : Stream")]
        [TestCase("(object) stream")]
        [TestCase("(object) Stream")]
        [TestCase("stream as object")]
        [TestCase("Stream as object")]
        public void InjectedAndCreatedField(string code)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);
        private readonly object stream;

        public Foo(Stream stream)
        {
            this.stream = stream ?? File.OpenRead(string.Empty);
        }


        public void Dispose()
        {
            ↓(this.stream as IDisposable)?.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("stream ?? File.OpenRead(string.Empty)", code);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("this.disposable.Dispose();")]
        [TestCase("this.disposable?.Dispose();")]
        [TestCase("disposable.Dispose();")]
        [TestCase("disposable?.Dispose();")]
        public void DisposingFieldAssignedWithInjected(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable arg)
        {
            this.disposable = arg;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("this.disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("this.disposable.Dispose();")]
        [TestCase("this.disposable?.Dispose();")]
        [TestCase("disposable.Dispose();")]
        [TestCase("disposable?.Dispose();")]
        public void DisposingPublicField(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        public IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("this.disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public void DisposingStaticField(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private static readonly IDisposable Disposable;

        public void Dispose()
        {
            ↓Disposable.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("Disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingPublicFieldOutsideOfLock()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly object gate;

        public IDisposable disposable;
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            var toDispose = (IDisposable)null;
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                toDispose = this.disposable;
                this.disposable = null;
            }

            ↓toDispose?.Dispose();
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public void DisposingPropertyAssignedWithInjected(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        public Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("public abstract Stream Stream { get; }")]
        [TestCase("public abstract Stream Stream { get; set; }")]
        [TestCase("public virtual Stream Stream { get; }")]
        [TestCase("public virtual Stream Stream { get; set; }")]
        public void DisposingAbstractOrVirtualProperty(string property)
        {
            var testCode = @"
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
                ↓this.Stream?.Dispose();
            }
        }
    }
}";

            testCode = testCode.AssertReplace("public abstract Stream Stream { get; }", property);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public void DisposingCalculatedPropertyNestedStatementBody(string disposeCall)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Bar : IDisposable
    {
        private readonly Foo foo;

        public Bar(Foo foo)
        {
            this.foo = foo;
        }

        public IDisposable Disposable
        {
            get
            {
               return this.foo.Disposable;
            }
        }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}";

            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, testCode);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public void DisposingCalculatedPropertyNestedExpressionBody(string disposeCall)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Bar : IDisposable
    {
        private readonly Foo foo;

        public Bar(Foo foo)
        {
            this.foo = foo;
        }

        public IDisposable Disposable => this.foo.Disposable;

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}";

            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, testCode);
        }

        [TestCase("this.foo.Disposable.Dispose()")]
        [TestCase("this.foo?.Disposable.Dispose()")]
        [TestCase("this.foo?.Disposable?.Dispose()")]
        [TestCase("this.foo.Disposable?.Dispose()")]
        public void DisposingNestedField(string disposeCall)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Bar : IDisposable
    {
        private readonly Foo foo;

        public Bar(Foo arg)
        {
            this.foo = arg;
        }

        public void Dispose()
        {
            ↓this.foo.Disposable.Dispose();
        }
    }
}";

            testCode = testCode.AssertReplace("this.foo.Disposable.Dispose()", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, testCode);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public void DisposingMutableProperty(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        public IDisposable Disposable { get; set; }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingCtorParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(IDisposable meh)
        {
            ↓meh.Dispose();
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar(IDisposable meh)
        {
            ↓meh.Dispose();
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingInjectedPropertyInBaseClass()
        {
            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private bool disposed = false;

        public FooBase()
            : this(null)
        {
        }

        public FooBase(object bar)
        {
            this.Bar = bar;
        }

        public object Bar { get; }

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

            var fooImplCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : FooBase
    {
        public Foo(Stream stream)
            : base(stream)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ↓(this.Bar as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooBaseCode, fooImplCode);
        }

        [Test]
        public void InjectedViaMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private IDisposable disposable;

        public void Meh(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingFieldInVirtualDispose()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

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
                ↓this.disposable.Dispose();
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
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void UsingField1()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
            using (↓disposable)
            {
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void UsingField2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
            using (var meh = ↓disposable)
            {
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("this.disposable.Dispose();")]
        [TestCase("this.disposable?.Dispose();")]
        [TestCase("disposable.Dispose();")]
        [TestCase("disposable?.Dispose();")]
        [TestCase("this.disposable.Disposable.Dispose();")]
        [TestCase("this.disposable?.Disposable.Dispose();")]
        [TestCase("this.disposable?.Disposable?.Dispose();")]
        [TestCase("disposable.Disposable.Dispose();")]
        [TestCase("disposable?.Disposable.Dispose();")]
        [TestCase("disposable?.Disposable?.Dispose();")]
        public void InjectedSingleAssignmentDisposable(string dispose)
        {
            var testCode = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class Foo : IDisposable
    {
        private readonly SingleAssignmentDisposable disposable;

        protected Foo(SingleAssignmentDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
     }
}";
            testCode = testCode.AssertReplace("this.disposable.Dispose();", dispose);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingArrayItemAssignedWithInjected()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable[] disposables = new IDisposable[1];

        public Foo(IDisposable disposable)
        {
            this.disposables[0] = disposable;
        }

        public void Bar()
        {
            var disposable = this.disposables[0];
            ↓disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingStaticArrayItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private static readonly IDisposable[] Disposables = new IDisposable[1];

        public void Bar()
        {
            var disposable = Disposables[0];
            ↓disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingDictionaryItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    using System.Collections.Generic;

    public sealed class Foo
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public Foo(IDisposable bar)
        {
            this.map[1] = bar;
        }

        public void Bar()
        {
            var disposable = map[0];
            ↓disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DisposingStaticDictionaryItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    using System.Collections.Generic;

    public sealed class Foo
    {
        private static readonly Dictionary<int, IDisposable> Map = new Dictionary<int, IDisposable>();

        public void Bar()
        {
            var disposable = Map[0];
            ↓disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
