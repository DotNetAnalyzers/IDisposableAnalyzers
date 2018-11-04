namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        private static readonly string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP008");

        [Test]
        public void PublicMethodRefParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public bool TryGetStream(ref Stream stream)
        {
            ↓stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            AnalyzerAssert.Diagnostics<AssignmentAnalyzer>(ExpectedDiagnostic, testCode);
        }
    }
}
