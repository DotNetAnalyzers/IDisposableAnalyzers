namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class WhenInjecting
        {
            [Test]
            public void FactoryMethodCallingPrivateCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly bool value;

        private Foo(bool value)
        {
            this.value = value;
        }

        public static Foo Create() => new Foo(true);
    }
}";
                AnalyzerAssert.Valid<IDISP006ImplementIDisposable>(testCode);
            }

            [Test]
            public void FactoryMethodCallingPrivateCtorWithCachedDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private static readonly IDisposable Cached = new Disposable();
        private readonly IDisposable value;

        private Foo(IDisposable value)
        {
            this.value = value;
        }

        public static Foo Create() => new Foo(Cached);
    }
}";
                AnalyzerAssert.Valid<IDISP006ImplementIDisposable>(DisposableCode, testCode);
            }
        }
    }
}