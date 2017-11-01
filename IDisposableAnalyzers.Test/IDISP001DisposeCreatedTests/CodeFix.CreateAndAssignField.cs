namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class CreateAndAssignField
        {
            [Test]
            public void LocalExplictTypeToFieldInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        internal Foo()
        {
            ↓Stream stream = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        private readonly Stream stream;

        internal Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP001DisposeCreated, CreateAndAssignFieldCodeFixProvider>(
                    testCode,
                    fixedCode);
                AnalyzerAssert.FixAll<IDISP001DisposeCreated, CreateAndAssignFieldCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void LocalVarToFieldInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public Foo()
        {
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly FileStream stream;

        public Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP001DisposeCreated, CreateAndAssignFieldCodeFixProvider>(
                    testCode,
                    fixedCode);
                AnalyzerAssert.FixAll<IDISP001DisposeCreated, CreateAndAssignFieldCodeFixProvider>(testCode, fixedCode);
            }
        }
    }
}