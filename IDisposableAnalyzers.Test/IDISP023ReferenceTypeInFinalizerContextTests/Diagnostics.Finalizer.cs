namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using System.Text;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class Finalizer
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FinalizerAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP023ReferenceTypeInFinalizerContext.Descriptor);

            private static readonly StringBuilder Builder = new StringBuilder();

            [TestCase("Builder.Append(1)")]
            [TestCase("Builder.Length")]
            public void Static(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class C
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        ~C()
        {
            ↓Builder.Append(1);
        }
    }
}".AssertReplace("Builder.Append(1)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.↓Builder.Append(1)")]
            [TestCase("↓Builder.Append(1)")]
            [TestCase("↓Builder.Length")]
            public void Instance(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class C
    {
        private readonly StringBuilder Builder = new StringBuilder();

        ~C()
        {
            this.↓Builder.Append(1);
        }
    }
}".AssertReplace("this.↓Builder.Append(1)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
