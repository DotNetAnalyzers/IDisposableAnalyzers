namespace IDisposableAnalyzers.Test.IDISP024DoNotCallSuppressFinalizeTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static class CodeFix
{
    private static readonly SuppressFinalizeAnalyzer Analyzer = new();
    private static readonly RemoveCallFix Fix = new();

    [Test]
    public static void SealedSimple()
    {
        var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
            ↓GC.SuppressFinalize(this);
        }
    }
}";

        var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, before, after);
    }
}
