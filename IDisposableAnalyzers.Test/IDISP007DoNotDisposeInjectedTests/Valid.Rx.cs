namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    [TestFixture(typeof(DisposeCallAnalyzer))]
    [TestFixture(typeof(LocalDeclarationAnalyzer))]
    [TestFixture(typeof(UsingStatementAnalyzer))]
    public static class ValidReactive<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new();

        [Test]
        public static void InjectedSubscribe()
        {
            var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly IDisposable subscription;

        public C(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ChainedCtorInjectedSubscribe()
        {
            var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public abstract class C : IDisposable
    {
        private readonly IDisposable subscription;

        public C(int no)
            : this(Create(no))
        {
        }

        public C(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }

        private static IObservable<object> Create(int i)
        {
            return Observable.Empty<object>();
        }
     }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedConditionalSubscribe()
        {
            var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly IDisposable subscription;

        public C(IObservable<object> observable)
        {
            this.subscription = observable?.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription?.Dispose();
        }
     }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SingleAssignmentDisposable()
        {
            var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected C()
        {
            this.subscription.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SingleAssignmentDisposableAssignedWithObservableSubscribe()
        {
            var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected C(IObservable<object> observable)
        {
            this.subscription.Disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SingleAssignmentDisposableAssignedInAction()
        {
            var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly Lazy<int> lazy;
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        private bool disposed;

        protected C()
        {
            this.lazy = new Lazy<int>(
                () =>
                    {
                        this.subscription.Disposable = File.OpenRead(string.Empty);
                        return 1;
                    });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
