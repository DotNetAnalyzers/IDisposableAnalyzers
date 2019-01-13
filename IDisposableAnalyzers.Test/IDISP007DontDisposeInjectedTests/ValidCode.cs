#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();

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

        [Test]
        public void DisposingArrayItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable[] disposables;

        public void Bar()
        {
            var disposable = disposables[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingDictionaryItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo : IDisposable
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public void Bar()
        {
            var disposable = map[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingWithBaseClass()
        {
            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class FooBase : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed = false;

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
                this.stream.Dispose();
            }
        }
    }
}";

            var fooImplCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class FooImpl : FooBase
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooBaseCode, fooImplCode);
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
        public Foo(int no)
            : this(no.ToString())
        {
        }

        public Foo(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.Bar as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooBaseCode, fooImplCode);
        }

        [Test]
        public void DisposingInjectedPropertyInBaseClassFieldExpressionBody()
        {
            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private readonly object bar;
        private bool disposed = false;

        public FooBase()
            : this(null)
        {
        }

        public FooBase(object bar)
        {
            this.bar = bar;
        }

        public object Bar => this.bar;

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
        public Foo(int no)
            : this(no.ToString())
        {
        }

        public Foo(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.Bar as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooBaseCode, fooImplCode);
        }

        [Test]
        public void DisposingInjectedPropertyInBaseClassFieldExpressionBodyNotAssignedByChained()
        {
            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private static IDisposable Empty = new Disposable();

        private readonly object bar;
        private bool disposed = false;

        public FooBase(string text)
        {
            this.bar = Empty;
        }

        public FooBase(object bar)
        {
            this.bar = bar;
        }

        public object Bar => this.bar;

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
        public Foo(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.Bar as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, fooBaseCode, fooImplCode);
        }

        [Test]
        public void InjectedInClassThatIsNotIDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedInClassThatIsIDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedInClassThatIsIDisposableManyCtors()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
            : this(disposable, ""meh"")
        {
        }

        public Foo(IDisposable disposable, IDisposable gah, int meh)
            : this(disposable, meh)
        {
        }

        private Foo(IDisposable disposable, int meh)
            : this(disposable, meh.ToString())
        {
        }

        private Foo(IDisposable disposable, string meh)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private object meh;

        public Foo(object meh)
        {
            this.meh = meh;
        }

        public void Dispose()
        {
            meh = null;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void NotDisposingFieldInVirtualDispose()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
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
        public void InjectingIntoPrivateCtor()
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

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, disposableCode, testCode);
        }

        [Test]
        public void BoolProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.ComponentModel;

    public sealed class Foo : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private bool isDirty;

        public Foo()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChanged?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool isDirty;

        public Foo()
        {
        }

        public void Bar(IDisposable meh)
        {
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreLambdaCreation()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Action<IDisposable> action = x => x.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("action(stream)")]
        [TestCase("action.Invoke(stream)")]
        public void IgnoreLambdaUsageOnLocal(string invokeCode)
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
            Action<IDisposable> action = x => x.Dispose();
            var stream = File.OpenRead(string.Empty);
            action(stream);
        }
    }
}".AssertReplace("action(stream)", invokeCode);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreInLambdaMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Meh(x => x.Dispose());
        }

        public static void Meh(Action<IDisposable> action)
        {
            action(null);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
