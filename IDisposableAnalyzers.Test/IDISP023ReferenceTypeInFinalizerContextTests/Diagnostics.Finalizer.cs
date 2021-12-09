namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Finalizer
        {
            private static readonly FinalizerAnalyzer Analyzer = new();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP023ReferenceTypeInFinalizerContext);

            [TestCase("↓Builder.Append(1)")]
            [TestCase("_ = ↓Builder.Length")]
            public static void Static(string expression)
            {
                var code = @"
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [TestCase("this.↓Builder.Append(1)")]
            [TestCase("↓Builder.Append(1)")]
            [TestCase("_ = ↓Builder.Length")]
            public static void Instance(string expression)
            {
                var code = @"
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void CallingStatic()
            {
                var code = @"
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }
        }
    }
}
