namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Dispose
        {
            private static readonly DisposeMethodAnalyzer Analyzer = new();

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
            public static void TouchingInstanceReferenceTypeInIfBlock()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                    this.builder.Clear();
                }
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void TouchingInstanceReferenceTypeInIfNotDiposedAndDispsing()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed && disposing)
            {
                this.isDisposed = true;
                this.builder.Clear();
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void TouchingInstanceReferenceTypeInIfDispsingAndNotDiposed()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !this.isDisposed)
            {
                this.isDisposed = true;
                this.builder.Clear();
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void TouchingStaticReferenceTypeInIfBlock()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                    Builder.Clear();
                }
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void TouchingReferenceTypeInIfExpression()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                    Builder.Append(1);
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("isDisposed.Equals(false)")]
            [TestCase("isDisposed.Equals(this)")]
            public static void TouchingStruct(string expression)
            {
                var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                _ = isDisposed.Equals(false);
            }
        }
    }
}".AssertReplace("isDisposed.Equals(false)", expression);
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void SettingStaticToNull()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public class C : IDisposable
    {
        private static StringBuilder? Builder = new StringBuilder();
        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                Builder = null;
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void SettingInstanceToNull()
            {
                var code = @"
namespace N
{
    using System;
    using System.Text;

    public class C : IDisposable
    {
        private StringBuilder? Builder = new StringBuilder();
        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                this.Builder = null;
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void WhenCallingBaseDispose()
            {
                var baseClass = @"
namespace N
{
    using System;

    public abstract class BaseClass : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
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
                this.disposable.Dispose();
            }
        }
    }
}";
                var code = @"
namespace N
{
    public class C : BaseClass
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, code);
            }

            [Test]
            public static void WhenCallingBaseDisposeAfterCheckDispose()
            {
                var baseClass = @"
namespace N
{
    using System;

    public abstract class BaseClass : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
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
                this.disposable.Dispose();
            }
        }
    }
}";
                var code = @"
namespace N
{
    public class C : BaseClass
    {
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, code);
            }

            [Test]
            public static void WhenCallingBaseDisposeAfterCheckDisposeAndIfDisposing()
            {
                var baseClass = @"
namespace N
{
    using System;

    public abstract class BaseClass : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
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
                this.disposable.Dispose();
            }
        }
    }
}";
                var code = @"
namespace N
{
    using System;

    public class C : BaseClass
    {
        private readonly IDisposable disposable = new Disposable();
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
                this.disposable.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, code);
            }

            [Test]
            public static void IfNotDisposingReturn()
            {
                var code = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

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
            if (!disposing)
                return;

            disposable.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, code);
            }

            [Test]
            public static void UsingStaticMember()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                // free unmanaged resources
                if (File.Exists(""abc""))
                {
                    File.Delete(""abc"");
                }

                disposedValue = true;
            }
        }

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, code);
            }
        }
    }
}
