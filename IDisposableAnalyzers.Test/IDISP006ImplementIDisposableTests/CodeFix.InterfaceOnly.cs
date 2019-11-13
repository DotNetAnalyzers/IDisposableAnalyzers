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
                var before = @"
namespace RoslynSandbox
{
    using System;

    public struct C : ↓IDisposable
    {
    }
}";

                var after = @"
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
                RoslynAssert.CodeFix(Fix, CS0535, before, after);
                RoslynAssert.FixAll(Fix, CS0535, before, after);
            }

            [Test]
            public static void NestedStruct()
            {
                var before = @"
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

                var after = @"
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
                RoslynAssert.CodeFix(Fix, CS0535, before, after);
                RoslynAssert.FixAll(Fix, CS0535, before, after);
            }
        }
    }
}
