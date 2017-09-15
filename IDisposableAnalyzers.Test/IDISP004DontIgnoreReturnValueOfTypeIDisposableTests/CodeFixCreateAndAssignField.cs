namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFixCreateAndAssignField
    {
        [Test]
        public void AssignIgnoredReturnValueToFieldInCtor()
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
            ↓File.OpenRead(string.Empty);
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
        private readonly IDisposable disposable;

        internal Foo()
        {
            this.disposable = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, CreateAndAssignFieldCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, CreateAndAssignFieldCodeFixProvider>(testCode, fixedCode);
        }
    }
}