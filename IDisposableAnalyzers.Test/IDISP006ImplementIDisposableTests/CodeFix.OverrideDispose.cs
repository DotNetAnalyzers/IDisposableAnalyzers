namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class OverrideDispose
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly CodeFixProvider Fix = new ImplementIDisposableFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP006");

            [Test]
            public static void SubclassStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C : StreamReader
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public C(string path)
            : base(path)
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C : StreamReader
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public C(string path)
            : base(path)
        {
        }

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

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void StyleCopCallingBaseThrowIfDisposed()
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

        protected virtual void ThrowIfDisposed()
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

    public class C : BaseClass
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C : BaseClass
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

        protected override void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }

            base.ThrowIfDisposed();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public static void UnderscoreWhenThrowIsNotVirtual()
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

    public class C : BaseClass
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C : BaseClass
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public static void UnderscoreWhenThrowIsVirtual()
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

        protected virtual void ThrowIfDisposed()
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

    public class C : BaseClass
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C : BaseClass
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

        protected override void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new System.ObjectDisposedException(GetType().FullName);
            }

            base.ThrowIfDisposed();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public static void SubclassingNinjectModule()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using Ninject.Modules;

    internal class C : NinjectModule
    {
        ↓private readonly IDisposable disposable = new Disposable();

        public override void Load()
        {
            throw new NotImplementedException();
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using Ninject.Modules;

    internal class C : NinjectModule
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        public override void Load()
        {
            throw new NotImplementedException();
        }

        public override void Dispose(bool disposing)
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

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            }
        }
    }
}
