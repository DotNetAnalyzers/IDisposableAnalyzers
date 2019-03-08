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

    public sealed class C : IDisposable
    {
        private readonly IDisposable[] disposables;

        public void M()
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

    public sealed class C : IDisposable
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public void M()
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

    public abstract class CBase : IDisposable
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

    public class CImpl : CBase
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
        public C(int no)
            : this(no.ToString())
        {
        }

        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.M as IDisposable)?.Dispose();
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

    public class CBase : IDisposable
    {
        private readonly object bar;
        private bool disposed = false;

        public CBase()
            : this(null)
        {
        }

        public CBase(object bar)
        {
            this.bar = bar;
        }

        public object M => this.bar;

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
        public C(int no)
            : this(no.ToString())
        {
        }

        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.M as IDisposable)?.Dispose();
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

    public class CBase : IDisposable
    {
        private static IDisposable Empty = new Disposable();

        private readonly object bar;
        private bool disposed = false;

        public CBase(string text)
        {
            this.bar = Empty;
        }

        public CBase(object bar)
        {
            this.bar = bar;
        }

        public object M => this.bar;

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
        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.M as IDisposable)?.Dispose();
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

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
            : this(disposable, ""meh"")
        {
        }

        public C(IDisposable disposable, IDisposable gah, int meh)
            : this(disposable, meh)
        {
        }

        private C(IDisposable disposable, int meh)
            : this(disposable, meh.ToString())
        {
        }

        private C(IDisposable disposable, string meh)
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

    public sealed class C : IDisposable
    {
        private object meh;

        public C(object meh)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
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

    public sealed class C : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private bool isDirty;

        public C()
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

    public sealed class C : IDisposable
    {
        private bool isDirty;

        public C()
        {
        }

        public void M(IDisposable meh)
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

    public class C
    {
        public C()
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

    public class C
    {
        public C()
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

    public class C
    {
        public C()
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
