namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Rx
        {
            [Test]
            public void SingleAssignmentDisposable()
            {
                var testCode = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class Foo : IDisposable
    {
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected Foo()
        {
            this.subscription.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
                AnalyzerAssert.Valid<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
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

    public abstract class Foo : IDisposable
    {
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected Foo(IObservable<object> observable)
        {
            this.subscription.Disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
                AnalyzerAssert.Valid<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
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

    public abstract class Foo : IDisposable
    {
        private readonly Lazy<int> lazy;
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        private bool disposed;

        protected Foo()
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
                AnalyzerAssert.Valid<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
            }
        }
    }
}