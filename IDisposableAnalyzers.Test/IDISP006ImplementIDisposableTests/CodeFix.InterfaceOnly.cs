namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class InterfaceOnly
        {
            private static readonly CodeFixProvider Fix = new ImplementIDisposableFix();
            //// ReSharper disable once InconsistentNaming
            private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

            [Test]
            public static void Struct()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public struct C : ↓IDisposable
    {
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public struct C : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
                RoslynAssert.FixAll(Fix, CS0535, testCode, fixedCode);
            }

            [Test]
            public static void NestedStruct()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class Cache<TKey, TValue>
    {
        internal struct Transaction : ↓IDisposable
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
                RoslynAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
                RoslynAssert.FixAll(Fix, CS0535, testCode, fixedCode);
            }
        }
    }
}
