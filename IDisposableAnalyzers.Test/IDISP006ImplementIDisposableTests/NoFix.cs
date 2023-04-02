namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static class NoFix
{
    private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new();
    private static readonly ImplementIDisposableFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP006ImplementIDisposable);

    [Test]
    public static void FieldWhenInterfaceIsMissing()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

        RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, code);
    }

    [Test]
    public static void PropertyWhenInterfaceIsMissing()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

        RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, code);
    }
}
