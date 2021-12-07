namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class NoFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
        private static readonly CodeFixProvider AddUsingFix = new AddUsingFix();
        private static readonly CodeFixProvider CreateAndAssignFieldFix = new CreateAndAssignFieldFix();
        private static readonly CodeFixProvider AddToCompositeDisposableFix = new AddToCompositeDisposableFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP004DoNotIgnoreCreated);

        [Test]
        public static void WhenArgument()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        internal static string? M()
        {
            return Meh(↓File.OpenRead(string.Empty));
        }

        private static string? Meh(Stream stream) => stream.ToString();
    }
}";

            RoslynAssert.NoFix(Analyzer, AddUsingFix, ExpectedDiagnostic, code);
            RoslynAssert.NoFix(Analyzer, CreateAndAssignFieldFix, ExpectedDiagnostic, code);
            RoslynAssert.NoFix(Analyzer, AddToCompositeDisposableFix, ExpectedDiagnostic, code);
        }
    }
}
