namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void DisposingVariable()
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
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public void DisposeBeforeAssigningInIfElse(string dispose)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh(bool b)
        {
            Stream stream = File.OpenRead(string.Empty);
            if (b)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }

            stream.Dispose();
        }
    }
}".AssertReplace("stream.Dispose();", dispose);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public void DisposeBeforeAssigningBeforeIfElse(string dispose)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh(bool b)
        {
            Stream stream = File.OpenRead(string.Empty);
            stream.Dispose();
            if (b)
            {
                stream = null;
            }
            else
            {
                stream = File.OpenRead(string.Empty);
            }

            stream?.Dispose();
        }
    }
}".AssertReplace("stream.Dispose();", dispose);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public void DisposeFieldBeforeIfElseReassigning(string dispose)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void Meh()
        {
            this.stream.Dispose();
            if (true)
            {
                this.stream = null;
            }
            else
            {
                this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}".AssertReplace("stream.Dispose();", dispose);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        public Foo()
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingFieldInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConditionallyDisposingFieldInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConditionallyDisposingUnderscoreFieldInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream _stream;

        public void Meh()
        {
            _stream?.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingUnderscoreFieldInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream _stream;

        public void Meh()
        {
            _stream.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssigningBackingFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
            private set { this.disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssigningSerialDisposableBackingFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SerialDisposable disposable = new SerialDisposable();

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable.Disposable; }
            private set { this.disposable.Disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssigningSerialDisposableFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SerialDisposable disposable = new SerialDisposable();

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable.Disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void DisposingPreviousAssignedToLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    class Foo : IDisposable
    {
        private Disposable current;

        public void Test()
        {
            var old = current;
            current = new Disposable();
            if (old != current) old.Dispose();
        }

        public void Dispose()
        {
            current?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public void WhileLoop(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo(int i)
        {
            Stream stream = File.OpenRead(string.Empty);
            while (i > 0)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
                i--;
            }

            stream.Dispose();
        }
    }
}".AssertReplace("stream.Dispose();", call);

            AnalyzerAssert.Valid(Analyzer, code);
        }
    }
}
