namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Rx
        {
            [Test]
            public static void SerialDisposable()
            {
                var code = @"
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

                RoslynAssert.Valid(Analyzer, code, settings: LibrarySettings.Reactive);
            }

            [Test]
            public static void FieldAssignedWithFileOpenReadDisposeWith()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    class C : IDisposable
    {
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();
        private readonly IDisposable disposable;

        public C()
        {
            this.disposable = File.OpenRead(string.Empty).DisposeWith(this.compositeDisposable);
        }

        public void Dispose()
        {
            this.compositeDisposable.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code, settings: LibrarySettings.Reactive);
            }
        }
    }
}
