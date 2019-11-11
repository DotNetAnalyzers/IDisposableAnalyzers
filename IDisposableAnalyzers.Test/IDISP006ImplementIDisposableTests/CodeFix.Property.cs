namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class Property
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly CodeFixProvider CodeFix = new ImplementIDisposableFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP006");

            [Test]
            public static void ImplementIDisposableAndMakeSealed()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C()
        {
        }

        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

        public int Value { get; }

        protected virtual void M()
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

    public sealed class C : System.IDisposable
    {
        private bool disposed;

        public C()
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

        private void M()
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

                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void ImplementIDisposableWithVirtualDisposeMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C()
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

        protected virtual void M()
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

    public class C : IDisposable
    {
        private bool disposed;

        public C()
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
            GC.SuppressFinalize(this);
        }

        protected virtual void M()
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
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private void Meh()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode, "Implement IDisposable with virtual dispose method.");
            }

            [Test]
            public static void ImplementIDisposableSealedClassUsingsInside()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C : System.IDisposable
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
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void ImplementIDisposableSealedClassUsingsOutside()
            {
                var testCode = @"
using System.IO;

namespace RoslynSandbox
{
    public sealed class C
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
using System.IO;

namespace RoslynSandbox
{
    public sealed class C : System.IDisposable
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
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void ImplementIDisposableSealedClassUnderscoreWithConst()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
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

    public sealed class C : System.IDisposable
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
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void ImplementIDisposableAbstractClass()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public abstract class C
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class C : IDisposable
    {
        private bool disposed;

        public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void WhenInterfaceIsMissing()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

                RoslynAssert.NoFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void FactoryMethodCallingPrivateCtorWithCreatedDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private C(IDisposable value)
        {
            this.Value = value;
        }

        ↓public IDisposable Value { get; }

        public static C Create() => new C(new Disposable());
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private bool disposed;

        private C(IDisposable value)
        {
            this.Value = value;
        }

        public IDisposable Value { get; }

        public static C Create() => new C(new Disposable());

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
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            }

            [Test]
            public static void Issue111PartialUserControl()
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
                RoslynAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode, "Implement IDisposable and make class sealed.");
            }
        }
    }
}
