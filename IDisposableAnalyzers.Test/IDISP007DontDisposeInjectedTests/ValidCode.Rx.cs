namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        public class Rx
        {
            [Test]
            public void InjectedSubscribe()
            {
                var testCode = @"
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void ChainedCtorInjectedSubscribe()
            {
                var testCode = @"
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void InjectedConditionalSubscribe()
            {
                var testCode = @"
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void SingleAssignmentDisposable()
            {
                var testCode = @"
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void SingleAssignmentDisposableAssignedWithObservableSubscribe()
            {
                var testCode = @"
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void SingleAssignmentDisposableAssignedInAction()
            {
                var testCode = @"
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
                RoslynAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
