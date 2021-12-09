namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class CreateAndAssignField
        {
            private static readonly LocalDeclarationAnalyzer Analyzer = new();
            private static readonly CreateAndAssignFieldFix Fix = new();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);

            [Test]
            public static void LocalExplicitTypeToFieldInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C
    {
        internal C()
        {
            ↓Stream stream = File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C
    {
        private readonly Stream stream;

        internal C()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalVarToFieldInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C()
        {
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly FileStream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
