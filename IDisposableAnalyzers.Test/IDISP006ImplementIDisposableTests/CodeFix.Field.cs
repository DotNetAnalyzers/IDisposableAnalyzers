namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class Field
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [Test]
            public static void SimpleImplementIDisposableAndMakeSealed()
            {
                var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
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

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void ImplementIDisposableAndMakeSealed()
            {
                var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public C()
        {
        }

        public int Value { get; }

        protected virtual void M1()
        {
        }

        private void M2()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public C()
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

        private void M1()
        {
        }

        private void M2()
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

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void ImplementIDisposableWithVirtualDisposeMethod()
            {
                var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public C()
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

        protected virtual void M()
        {
        }

        private void M1()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public C()
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

        private void M1()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "LEGACY Implement IDisposable with protected virtual dispose method.");
            }

            [Test]
            public static void ImplementIDisposableSealedClassUsingsInside()
            {
                var before = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void ImplementIDisposableSealedClassUsingsOutside()
            {
                var before = @"
using System.IO;

namespace N
{
    public sealed class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"using System;
using System.IO;

namespace N
{
    public sealed class C : IDisposable
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void ImplementIDisposableSealedClassUnderscore()
            {
                var before = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void ImplementIDisposableSealedClassUnderscoreWithConst()
            {
                var before = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public const int Value = 2;

        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AbstractClassImplementIDisposable()
            {
                var before = @"
namespace N
{
    using System.IO;

    public abstract class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public virtual void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Implement IDisposable.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Implement IDisposable.");
            }

            [Test]
            public static void AbstractClassImplementIDisposableLegacyPattern()
            {
                var before = @"
namespace N
{
    using System.IO;

    public abstract class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "LEGACY Implement IDisposable with protected virtual dispose method.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "LEGACY Implement IDisposable with protected virtual dispose method.");
            }

            [Test]
            public static void ImplementIDisposableAbstractClassUnderscore()
            {
                var before = @"
namespace N
{
    using System.IO;

    public abstract class C
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class C : IDisposable
    {
        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "LEGACY Implement IDisposable with protected virtual dispose method.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "LEGACY Implement IDisposable with protected virtual dispose method.");
            }

            [Test]
            public static void FactoryMethodCallingPrivateCtorWithCreatedDisposable()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C
    {
        ↓private readonly IDisposable value;

        private C(IDisposable value)
        {
            this.value = value;
        }

        public static C Create() => new C(new Disposable());
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable value;
        private bool disposed;

        private C(IDisposable value)
        {
            this.value = value;
        }

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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void Issue111PartialUserControl()
            {
                var before = @"
namespace N
{
    using System.Windows.Controls;

    public partial class CodeTabView : UserControl
    {
        ↓private readonly Disposable disposable = new Disposable();
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.Windows.Controls;

    public sealed partial class CodeTabView : UserControl, IDisposable
    {
        private readonly Disposable disposable = new Disposable();
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after, "Implement IDisposable and make class sealed.");
            }
        }
    }
}
