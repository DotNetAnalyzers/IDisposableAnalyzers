namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Finalizer
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FinalizerAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP023ReferenceTypeInFinalizerContext);

            [TestCase("↓Builder.Append(1)")]
            [TestCase("_ = ↓Builder.Length")]
            public static void Static(string expression)
            {
                var testCode = @"
namespace N
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
}".AssertReplace("↓Builder.Append(1)", expression);

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.↓Builder.Append(1)")]
            [TestCase("↓Builder.Append(1)")]
            [TestCase("_ = ↓Builder.Length")]
            public static void Instance(string expression)
            {
                var testCode = @"
namespace N
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void CallingStatic()
            {
                var testCode = @"
namespace N
{
    using System.Text;

    public class C
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        ~C()
        {
            ↓M();
        }

        private static void M() => Builder.Append(1);
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
