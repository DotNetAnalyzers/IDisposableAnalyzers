namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Argument
        {
            [Test]
            public void ChainedCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void ChainedCtorCoalesce()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.Disposable = disposable ?? disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void ChainedCtors()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly int meh;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
            : this(disposable, 1)
        {
        }

        private Foo(IDisposable disposable, int meh)
        {
            this.meh = meh;
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void ChainedCtorCallsBaseCtorDisposedInThis()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        protected FooBase(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable Disposable => this.disposable;

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

                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : FooBase
    {
        private bool disposed;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
            : base(disposable)
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
                this.Disposable.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, baseCode, testCode);
            }

            [Test]
            public void ChainedBaseCtorDisposedInThis()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private readonly object disposable;
        private bool disposed;

        protected FooBase(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public object Bar
        {
            get
            {
                return this.disposable;
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
        }
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : FooBase
    {
        private bool disposed;

        public Foo()
            : base(new Disposable())
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
                (this.Bar as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, baseCode, testCode);
            }

            [Test]
            public void StreamInStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public string Bar()
        {
            using (var reader = new StreamReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposableCreate()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using (System.Reactive.Disposables.Disposable.Create(() => File.OpenRead(string.Empty)))
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void NewStandardKernelNewModuleArgument()
            {
                var modulCode = @"
namespace RoslynSandbox
{
    using System;
    using Ninject.Modules;

    public class FooModule : NinjectModule
    {
        public override void Load()
        {
            throw new NotImplementedException();
        }
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    using Ninject;

    public sealed class Foo
    {
        public Foo()
        {
            using (new StandardKernel(new FooModule()))
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, modulCode, testCode);
            }

            [Test]
            public void MethodReturningStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public string Bar()
        {
            using (var reader = GetReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }

        private static StreamReader GetReader(Stream stream)
        {
            return new StreamReader(stream);
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void MethodReturningStreamReaderExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public string Bar()
        {
            using (var reader = GetReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }

        private static StreamReader GetReader(Stream stream) => new StreamReader(stream);
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void MethodWithFuncTaskAsParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;
    public class Foo
    {
        public void Meh()
        {
            this.Bar(() => Task.Delay(0));
        }
        public void Bar(Func<Task> func)
        {
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void MethodWithFuncStreamAsParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            this.Bar(() => File.OpenRead(string.Empty));
        }

        public void Bar(Func<Stream> func)
        {
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
