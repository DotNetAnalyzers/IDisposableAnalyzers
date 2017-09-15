namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposable>
    {
        internal class Rx : NestedHappyPathVerifier<HappyPath>
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

                AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
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

                AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
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

                AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
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

                AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
            }
        }
    }
}