namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        private static readonly string DisposableCode = @"
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
        public void ImplementIDisposableAndMakeSealed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
        }

        public int Value { get; }

        protected virtual void Bar()
        {
        }

        private void Meh()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public Foo()
        {
        }

        public int Value { get; }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void Bar()
        {
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private void Meh()
        {
        }
    }
}";

            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode, "Implement IDisposable and make class sealed.");
        }

        [Test]
        public void ImplementIDisposableWithVirtualDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
        }

        public int Value { get; }

        public int this[int value]
        {
            get
            {
                return value;
            }
        }

        protected virtual void Bar()
        {
        }

        private void Meh()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public Foo()
        {
        }

        public int Value { get; }

        public int this[int value]
        {
            get
            {
                return value;
            }
        }

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
            }
        }

        protected virtual void Bar()
        {
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private void Meh()
        {
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode, "Implement IDisposable with virtual dispose method.");
        }

        [Test]
        public void ImplementIDisposableSealedClassUsingsInside()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ImplementIDisposableSealedClassUsingsOutside()
        {
            var testCode = @"
using System.IO;
namespace RoslynSandbox
{
    public sealed class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"using System;
using System.IO;
namespace RoslynSandbox
{
    public sealed class Foo : IDisposable
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
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ImplementIDisposableSealedClassUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ImplementIDisposableSealedClassUnderscoreWithConst()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public const int Value = 2;

        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public const int Value = 2;

        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ImplementIDisposableAbstractClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public abstract class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

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
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ImplementIDisposableAbstractClassUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public abstract class Foo
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class Foo : IDisposable
    {
        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ImplementIDisposableWhenInterfaceIsMissing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode, "Implement IDisposable and make class sealed.");
        }

        [Test]
        public void ImplementIDisposableWithProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
        }

        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private bool disposed;

        public Foo()
        {
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode, "Implement IDisposable and make class sealed.");
        }

        [Test]
        public void OverrideDispose()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool disposed;

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
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
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
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, new[] { baseCode, fixedCode });
        }

        [Test]
        public void OverrideDisposeUnderscore()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, new[] { baseCode, fixedCode });
        }

        [Test]
        public void FactoryMethodCallingPrivateCtorWithCreatedDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        ↓private readonly IDisposable value;

        private Foo(IDisposable value)
        {
            this.value = value;
        }

        public static Foo Create() => new Foo(new Disposable());
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable value;
        private bool disposed;

        private Foo(IDisposable value)
        {
            this.value = value;
        }

        public static Foo Create() => new Foo(new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.value?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode });
        }

        [Test]
        public void Issue111PartialUserControl()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public partial class CodeTabView : UserControl
    {
        ↓private readonly RoslynSandbox.Disposable disposable = new RoslynSandbox.Disposable();
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Windows.Controls;

    public sealed partial class CodeTabView : UserControl, IDisposable
    {
        ↓private readonly RoslynSandbox.Disposable disposable = new RoslynSandbox.Disposable();


        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.disposable.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode, "Implement IDisposable and make class sealed.");
        }
    }
}