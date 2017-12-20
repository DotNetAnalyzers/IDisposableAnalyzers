namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class Property
        {
            private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly ImplementIDisposableCodeFixProvider CodeFix = new ImplementIDisposableCodeFixProvider();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP006");

            [Test]
            public void ImplementIDisposableAndMakeSealed()
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
    using System.IO;

    public sealed class Foo : System.IDisposable
    {
        private bool disposed;

        public Foo()
        {
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);

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

        private void Meh()
        {
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";

                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode, "Implement IDisposable and make class sealed.");
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
        public Foo()
        {
        }

        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

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
    using System.IO;

    public class Foo : System.IDisposable
    {
        private bool disposed;

        public Foo()
        {
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);

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

        protected virtual void Bar()
        {
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

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }

        private void Meh()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode, "Implement IDisposable with virtual dispose method.");
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
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo : System.IDisposable
    {
        private bool disposed;

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
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
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
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
using System.IO;

namespace RoslynSandbox
{
    public sealed class Foo : System.IDisposable
    {
        private bool disposed;

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
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
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
        private readonly int _value = 1;

        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo : System.IDisposable
    {
        public const int Value = 2;
        private readonly int _value = 1;
        private bool _disposed;

        public Stream Stream { get; } = File.OpenRead(string.Empty);

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
                throw new System.ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
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
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public abstract class Foo : System.IDisposable
    {
        private bool disposed;

        public Stream Stream { get; } = File.OpenRead(string.Empty);

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

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void WhenInterfaceIsMissing()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

                AnalyzerAssert.NoFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode);
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
        private Foo(IDisposable value)
        {
            this.Value = value;
        }

        ↓public IDisposable Value { get; }

        public static Foo Create() => new Foo(new Disposable());
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool disposed;

        private Foo(IDisposable value)
        {
            this.Value = value;
        }

        public IDisposable Value { get; }

        public static Foo Create() => new Foo(new Disposable());

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
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void Issue111PartialUserControl()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Windows.Controls;

    public partial class CodeTabView : UserControl
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Windows.Controls;

    public partial sealed class CodeTabView : UserControl, System.IDisposable
    {
        private bool disposed;

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
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode, "Implement IDisposable and make class sealed.");
            }
        }
    }
}
