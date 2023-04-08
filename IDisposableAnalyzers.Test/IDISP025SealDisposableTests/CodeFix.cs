namespace IDisposableAnalyzers.Test.IDISP025SealDisposableTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static class CodeFix
{
    private static readonly ClassDeclarationAnalyzer Analyzer = new();
    private static readonly SealFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP025SealDisposable);

    [Test]
    public static void IDisposable()
    {
        var before = """
            namespace N;

            using System;

            public class ↓C : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """;

        var after = """
            namespace N

            using System;

            public sealed class C : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """;
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}
