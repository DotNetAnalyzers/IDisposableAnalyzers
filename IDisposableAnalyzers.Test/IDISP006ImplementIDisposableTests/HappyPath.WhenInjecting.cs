namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP006ImplementIDisposable>
    {
        internal class WhenInjecting : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task FactoryMethodCallingPrivateCtor()
            {
                var testCode = @"
    public class Foo
    {
        private readonly bool value;

        private Foo(bool value)
        {
            this.value = value;
        }

        public static Foo Create() => new Foo(true);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task FactoryMethodCallingPrivateCtorWithCachedDisposable()
            {
                var testCode = @"
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
}";
                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}