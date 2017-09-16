namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        public class InterfaceOnlyMakeSealed
        {
            [Test]
            public void ImplementIDisposableDisposeMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : ↓IDisposable
    {
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
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
                AnalyzerAssert.CodeFix<ImplementIDisposableCodeFixProvider>("CS0535", testCode, fixedCode, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public void ImplementIDisposableDisposeMethodWithProtectedPrivateSetProperty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : ↓IDisposable
    {
        protected int Value { get; private set; }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool disposed;

        private int Value { get; set; }

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
                AnalyzerAssert.CodeFix<ImplementIDisposableCodeFixProvider>("CS0535", testCode, fixedCode, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public void ImplementIDisposableDisposeMethodWithPublicVirtualMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : ↓IDisposable
    {
        public virtual void Bar()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool disposed;

        public void Bar()
        {
        }

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
                AnalyzerAssert.CodeFix<ImplementIDisposableCodeFixProvider>("CS0535", testCode, fixedCode, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public void ImplementIDisposableDisposeMethodWithProtectedVirtualMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : ↓IDisposable
    {
        protected virtual void Bar()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool disposed;

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
    }
}";
                AnalyzerAssert.CodeFix<ImplementIDisposableCodeFixProvider>("CS0535", testCode, fixedCode, "Implement IDisposable and make class sealed.");
            }
        }
    }
}