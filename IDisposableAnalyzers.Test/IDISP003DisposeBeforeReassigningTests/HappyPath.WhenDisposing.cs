namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(IDISP003DisposeBeforeReassigning))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    internal class HappyPathWhenDisposing<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();

#pragma warning disable SA1203 // Constants must appear before fields
        private const string DisposableCode = @"
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
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            if (true)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            testCode = testCode.AssertReplace("stream.Dispose();", dispose);
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
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            stream.Dispose();
            if (true)
            {
                stream = null;
            }
            else
            {
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            testCode = testCode.AssertReplace("stream.Dispose();", dispose);
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
}";
            testCode = testCode.AssertReplace("stream.Dispose();", dispose);
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
    }
}
