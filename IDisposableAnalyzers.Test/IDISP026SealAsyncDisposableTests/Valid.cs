namespace IDisposableAnalyzers.Test.IDISP026SealAsyncDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly ClassDeclarationAnalyzer Analyzer = new();

        public static class DisposeAsync
        {
            [Test]
            public static void SealedSimple()
            {
                var code = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public sealed class C : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void SealedPartial()
            {
                var part1 = @"
namespace N
{
    using System;

    public sealed partial class C : IAsyncDisposable
    {
    }
}";

                var part2 = @"
namespace N
{
    using System.Threading.Tasks;

    public sealed partial class C
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, part1, part2);
            }

            [Test]
            public static void WithDisposeAsyncCore()
            {
                var code = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            return ValueTask.CompletedTask;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
