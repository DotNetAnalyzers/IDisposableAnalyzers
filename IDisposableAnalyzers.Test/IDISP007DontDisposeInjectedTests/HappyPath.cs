namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP007DontDisposeInjected>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [Test]
        public async Task DisposingArrayItem()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingDictionaryItem()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingWithBaseClass()
        {
            var fooBaseCode = @"
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
    }";

            var fooImplCode = @"
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
    }";
            await this.VerifyHappyPathAsync(fooBaseCode, fooImplCode).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingInjectedPropertyInBaseClass()
        {
            var fooBaseCode = @"
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
    }";

            var fooImplCode = @"
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
    }";
            await this.VerifyHappyPathAsync(fooBaseCode, fooImplCode).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingInjectedPropertyInBaseClassFieldExpressionBody()
        {
            var fooBaseCode = @"
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
    }";

            var fooImplCode = @"
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
    }";
            await this.VerifyHappyPathAsync(fooBaseCode, fooImplCode).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingInjectedPropertyInBaseClassFieldExpressionBodyNotAssignedByChained()
        {
            var fooBaseCode = @"
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
    }";

            var fooImplCode = @"
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
    }";
            await this.VerifyHappyPathAsync(DisposableCode, fooBaseCode, fooImplCode).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsNotIDisposable()
        {
            var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposable()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposableManyCtors()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInVirtualDispose()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }";

            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(disposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task BoolProperty()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInMethod()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreLambdaCreation()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("action(stream)")]
        [TestCase("action.Invoke(stream)")]
        public async Task IgnoreLambdaUsageOnLocal(string invokeCode)
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
}";
            testCode = testCode.AssertReplace("action(stream)", invokeCode);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreInLambdaMethod()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}