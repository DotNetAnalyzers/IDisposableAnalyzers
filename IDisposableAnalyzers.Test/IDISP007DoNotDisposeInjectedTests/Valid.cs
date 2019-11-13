namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();

        private const string DisposableCode = @"
namespace N
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
        public static void DisposingArrayItem()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingDictionaryItem()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingWithBaseClass()
        {
            var fooBaseCode = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class Base : IDisposable
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
namespace N
{
    using System;
    using System.IO;

    public class CImpl : Base
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
            RoslynAssert.Valid(Analyzer, fooBaseCode, fooImplCode);
        }

        [Test]
        public static void DisposingInjectedPropertyInBaseClass()
        {
            var fooBaseCode = @"
namespace N
{
    using System;

    public class Base : IDisposable
    {
        private bool disposed = false;

        public Base()
            : this(null)
        {
        }

        public Base(object bar)
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
namespace N
{
    using System;
    using System.IO;

    public class C : Base
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
            RoslynAssert.Valid(Analyzer, fooBaseCode, fooImplCode);
        }

        [Test]
        public static void DisposingInjectedPropertyInBaseClassFieldExpressionBody()
        {
            var fooBaseCode = @"
namespace N
{
    using System;

    public class Base : IDisposable
    {
        private readonly object bar;
        private bool disposed = false;

        public Base()
            : this(null)
        {
        }

        public Base(object bar)
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
namespace N
{
    using System;
    using System.IO;

    public class C : Base
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
            RoslynAssert.Valid(Analyzer, fooBaseCode, fooImplCode);
        }

        [Test]
        public static void DisposingInjectedPropertyInBaseClassFieldExpressionBodyNotAssignedByChained()
        {
            var fooBaseCode = @"
namespace N
{
    using System;

    public class Base : IDisposable
    {
        private static IDisposable Empty = new Disposable();

        private readonly object bar;
        private bool disposed = false;

        public Base(string text)
        {
            this.bar = Empty;
        }

        public Base(object bar)
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
namespace N
{
    using System;
    using System.IO;

    public class C : Base
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
            RoslynAssert.Valid(Analyzer, DisposableCode, fooBaseCode, fooImplCode);
        }

        [Test]
        public static void InjectedInClassThatIsNotIDisposable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInClassThatIsIDisposable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInClassThatIsIDisposableManyCtors()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NotDisposingFieldInVirtualDispose()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, disposableCode, code);
        }

        [Test]
        public static void BoolProperty()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInMethod()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreLambdaCreation()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("action(stream)")]
        [TestCase("action.Invoke(stream)")]
        public static void IgnoreLambdaUsageOnLocal(string invokeCode)
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreInLambdaMethod()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignedParameter()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            disposable = File.OpenRead(string.Empty);
            disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignedParameterViaOut()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            if (TryReassign(disposable, out disposable))
            {
                disposable.Dispose();
            }
        }

        private static bool TryReassign(IDisposable old, out IDisposable result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
