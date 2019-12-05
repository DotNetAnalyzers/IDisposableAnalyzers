namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Inheritance
        {
            [Test]
            public static void WhenNotCallingBaseDispose()
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
        }
    }
}";

                RoslynAssert.Valid(Analyzer, Descriptor, DisposableCode, baseClass, code);
            }

            [Test]
            public static void WhenCallingBaseDisposeAfterIfDisposedReturn()
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
            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, Descriptor, DisposableCode, baseClass, code);
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
            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, Descriptor, DisposableCode, baseClass, code);
            }

            [Test]
            public static void WhenOverriddenIsNotVirtualDispose()
            {
                var baseClass = @"
namespace N
{
    using System;
    using System.IO;

    abstract class BaseClass : IDisposable
    {
        public void Dispose()
        {
            this.M();
        }

        protected abstract void M();
    }
}";

                var code = @"
namespace N
{
    using System;
    using System.IO;

    class C : BaseClass
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        protected override void M()
        {
            this.stream.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, Descriptor, baseClass, code);
            }

            [TestCase("this.components.Add(stream)")]
            [TestCase("components.Add(stream)")]
            public static void LocalAddedToFormComponents(string expression)
            {
                var code = @"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        Winform()
        {
            var stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(stream);
        }
    }
}".AssertReplace("this.components.Add(stream)", expression);
                RoslynAssert.NoAnalyzerDiagnostics(Analyzer, code);
            }

            [TestCase("this.components.Add(this.stream)")]
            [TestCase("components.Add(stream)")]
            public static void FieldAddedToFormComponents(string expression)
            {
                var code = @"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        private readonly Stream stream;

        Winform()
        {
            this.stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(this.stream);
        }
    }
}".AssertReplace("this.components.Add(this.stream)", expression);
                RoslynAssert.NoAnalyzerDiagnostics(Analyzer, code);
            }
        }
    }
}
