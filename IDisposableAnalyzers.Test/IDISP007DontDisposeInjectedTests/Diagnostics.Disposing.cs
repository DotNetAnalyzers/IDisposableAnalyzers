namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Disposing
        {
            private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP007");

            [TestCase("(object) stream")]
            [TestCase("(object) Stream")]
            [TestCase("stream as object")]
            [TestCase("Stream as object")]
            public static void InjectedAndCreatedField(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);
        private readonly object stream;

        public C(Stream stream)
        {
            this.stream = stream ?? File.OpenRead(string.Empty);
        }


        public void Dispose()
        {
            ↓(this.stream as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("stream ?? File.OpenRead(string.Empty)", code);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.disposable.Dispose();")]
            [TestCase("this.disposable?.Dispose();")]
            [TestCase("disposable.Dispose();")]
            [TestCase("disposable?.Dispose();")]
            public static void DisposingFieldAssignedWithInjected(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable arg)
        {
            this.disposable = arg;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }
}".AssertReplace("this.disposable.Dispose();", disposeCall);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.disposable.Dispose();")]
            [TestCase("this.disposable?.Dispose();")]
            [TestCase("disposable.Dispose();")]
            [TestCase("disposable?.Dispose();")]
            public static void DisposingPublicField(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        public IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }
}".AssertReplace("this.disposable.Dispose();", disposeCall);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("Disposable.Dispose();")]
            [TestCase("Disposable?.Dispose();")]
            public static void DisposingStaticField(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private static readonly IDisposable Disposable;

        public void Dispose()
        {
            ↓Disposable.Dispose();
        }
    }
}".AssertReplace("Disposable.Dispose();", disposeCall);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingPublicFieldOutsideOfLock()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.Disposable.Dispose();")]
            [TestCase("this.Disposable?.Dispose();")]
            [TestCase("Disposable.Dispose();")]
            [TestCase("Disposable?.Dispose();")]
            public static void DisposingPropertyAssignedWithInjected(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        public C(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}".AssertReplace("this.Disposable.Dispose();", disposeCall);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("public abstract Stream Stream { get; }")]
            [TestCase("public abstract Stream Stream { get; set; }")]
            [TestCase("public virtual Stream Stream { get; }")]
            [TestCase("public virtual Stream Stream { get; set; }")]
            public static void DisposingAbstractOrVirtualProperty(string property)
            {
                var testCode = @"
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
                ↓this.Stream?.Dispose();
            }
        }
    }
}".AssertReplace("public abstract Stream Stream { get; }", property);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.Disposable.Dispose();")]
            [TestCase("this.Disposable?.Dispose();")]
            [TestCase("Disposable.Dispose();")]
            [TestCase("Disposable?.Dispose();")]
            public static void DisposingCalculatedPropertyNestedStatementBody(string disposeCall)
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public C(IDisposable disposable)
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

    public sealed class M : IDisposable
    {
        private readonly C foo;

        public M(C foo)
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
}".AssertReplace("this.Disposable.Dispose();", disposeCall);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, testCode);
            }

            [TestCase("this.Disposable.Dispose();")]
            [TestCase("this.Disposable?.Dispose();")]
            [TestCase("Disposable.Dispose();")]
            [TestCase("Disposable?.Dispose();")]
            public static void DisposingCalculatedPropertyNestedExpressionBody(string disposeCall)
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public C(IDisposable disposable)
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

    public sealed class M : IDisposable
    {
        private readonly C foo;

        public M(C foo)
        {
            this.foo = foo;
        }

        public IDisposable Disposable => this.foo.Disposable;

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}".AssertReplace("this.Disposable.Dispose();", disposeCall);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, testCode);
            }

            [TestCase("this.foo.Disposable.Dispose()")]
            [TestCase("this.foo?.Disposable.Dispose()")]
            [TestCase("this.foo?.Disposable?.Dispose()")]
            [TestCase("this.foo.Disposable?.Dispose()")]
            public static void DisposingNestedField(string disposeCall)
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public C(IDisposable disposable)
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

    public sealed class M : IDisposable
    {
        private readonly C foo;

        public M(C arg)
        {
            this.foo = arg;
        }

        public void Dispose()
        {
            ↓this.foo.Disposable.Dispose();
        }
    }
}".AssertReplace("this.foo.Disposable.Dispose()", disposeCall);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, testCode);
            }

            [TestCase("this.Disposable.Dispose();")]
            [TestCase("this.Disposable?.Dispose();")]
            [TestCase("Disposable.Dispose();")]
            [TestCase("Disposable?.Dispose();")]
            public static void DisposingMutableProperty(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        public IDisposable Disposable { get; set; }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }
}".AssertReplace("this.Disposable.Dispose();", disposeCall);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingCtorParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C(IDisposable meh)
        {
            ↓meh.Dispose();
        }
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public void M(IDisposable meh)
        {
            ↓meh.Dispose();
        }
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingInjectedPropertyInBaseClass()
            {
                var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;

    public class CBase : IDisposable
    {
        private bool disposed = false;

        public CBase()
            : this(null)
        {
        }

        public CBase(object bar)
        {
            this.M = bar;
        }

        public object M { get; }

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

    public class C : CBase
    {
        public C(Stream stream)
            : base(stream)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ↓(this.M as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooBaseCode, fooImplCode);
            }

            [Test]
            public static void InjectedViaMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingFieldInVirtualDispose()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        public C(IDisposable disposable)
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
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
            public static void InjectedSingleAssignmentDisposable(string dispose)
            {
                var testCode = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly SingleAssignmentDisposable disposable;

        protected C(SingleAssignmentDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
     }
}".AssertReplace("this.disposable.Dispose();", dispose);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingArrayItemAssignedWithInjected()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable[] disposables = new IDisposable[1];

        public C(IDisposable disposable)
        {
            this.disposables[0] = disposable;
        }

        public void M()
        {
            var disposable = this.disposables[0];
            ↓disposable.Dispose();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingStaticArrayItem()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private static readonly IDisposable[] Disposables = new IDisposable[1];

        public void M()
        {
            var disposable = Disposables[0];
            ↓disposable.Dispose();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingDictionaryItem()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    using System.Collections.Generic;

    public sealed class C
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public C(IDisposable bar)
        {
            this.map[1] = bar;
        }

        public void M()
        {
            var disposable = map[0];
            ↓disposable.Dispose();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void DisposingStaticDictionaryItem()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    using System.Collections.Generic;

    public sealed class C
    {
        private static readonly Dictionary<int, IDisposable> Map = new Dictionary<int, IDisposable>();

        public void M()
        {
            var disposable = Map[0];
            ↓disposable.Dispose();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("((IDisposable)o).Dispose()")]
            [TestCase("((IDisposable)o)?.Dispose()")]
            [TestCase("(o as IDisposable).Dispose().Dispose()")]
            [TestCase("(o as IDisposable).Dispose()?.Dispose()")]
            public static void Cast(string cast)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static void M(object o)
        {
            ↓((IDisposable)o).Dispose();
        }
    }
}".AssertReplace("((IDisposable)o).Dispose()", cast);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void IfPatternMatchedInjected()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static void M(object o)
        {
            if (o is IDisposable disposable)
            {
                ↓disposable.Dispose();
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void SwitchPatternMatchedInjected()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static void M(object o)
        {
            switch (o)
            {
                case IDisposable disposable:
                ↓disposable.Dispose();
                break;
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
