namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        public class InterfaceOnly
        {
            // ReSharper disable once InconsistentNaming
            private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

            [Test]
            public void Struct()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public struct Foo : IDisposable
    {
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public struct Foo : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix<ImplementIDisposableCodeFixProvider>(CS0535, testCode, fixedCode);
                AnalyzerAssert.FixAll<ImplementIDisposableCodeFixProvider>(CS0535, testCode, fixedCode);
            }

            [Test]
            public void NestedStruct()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class Cache<TKey, TValue>
    {
        internal struct Transaction : IDisposable
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class Cache<TKey, TValue>
    {
        internal struct Transaction : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}";
                AnalyzerAssert.CodeFix<ImplementIDisposableCodeFixProvider>(CS0535, testCode, fixedCode);
                AnalyzerAssert.FixAll<ImplementIDisposableCodeFixProvider>(CS0535, testCode, fixedCode);
            }
        }
    }
}
