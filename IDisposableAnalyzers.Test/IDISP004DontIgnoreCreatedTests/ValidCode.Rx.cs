namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        [Test]
        public void SerialDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly SerialDisposable disposable = new SerialDisposable();

        public void Update()
        {
            this.disposable.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SingleAssignmentDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();

        public void Update()
        {
            this.disposable.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable;

        public Foo()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty),
            };
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable;

        public Foo()
        {
            this.disposable = new CompositeDisposable(
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableAddIObservableSubscribe()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public Foo(IObservable<object> observable)
        {
            this.disposable.Add(observable.Subscribe(_ => { }));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableAddNewSingleAssignmentDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public Foo()
        {
            this.disposable.Add(new SingleAssignmentDisposable());
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableAddThrottleSubscribe()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public Foo(IObservable<object> observable)
        {
            this.disposable.Add(observable.Throttle(TimeSpan.FromMilliseconds(100))
                                          .Subscribe(_ => { }));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableExtAddAndReturn()
        {
            var compositeDisposableExtCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        internal string AddAndReturnToString()
        {
            return disposable.AddAndReturn(new Disposable()).ToString();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, compositeDisposableExtCode, testCode);
        }
    }
}
