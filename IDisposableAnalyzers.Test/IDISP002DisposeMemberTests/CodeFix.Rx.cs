namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class Rx
        {
            private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new();
            private static readonly DisposeMemberFix Fix = new();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP002DisposeMember);

            [Test]
            public static void SerialDisposable()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        ↓private readonly SerialDisposable disposable = new SerialDisposable();

        public void Update()
        {
            this.disposable.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, settings: LibrarySettings.Reactive);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, settings: LibrarySettings.Reactive);
            }

            [Test]
            public static void ObservableSubscribe()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, settings: LibrarySettings.Reactive);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, settings: LibrarySettings.Reactive);
            }

            [Test]
            public static void ObservableElvisSubscribe()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable? disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable?.Subscribe(_ => { });
        }

        public void Dispose()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable? disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable?.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, settings: LibrarySettings.Reactive);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, settings: LibrarySettings.Reactive);
            }
        }
    }
}
