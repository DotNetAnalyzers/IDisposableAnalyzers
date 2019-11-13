namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class CreateAndAssignField
        {
            private static readonly CodeFixProvider Fix = new CreateAndAssignFieldFix();

            [Test]
            public static void AssignIgnoredReturnValueToFieldInCtorWhenEmpty()
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
            ↓File.OpenRead(string.Empty);
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
        private readonly IDisposable disposable;

        internal C()
        {
            this.disposable = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AssignIgnoredReturnValueToFieldInCtorWhenUsesThis()
            {
                var before = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private readonly int value;

        public C(int value)
        {
            this.value = value;
            ↓File.OpenRead(string.Empty);
        }

        public int Value => this.value;
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private readonly int value;
        private readonly System.IDisposable disposable;

        public C(int value)
        {
            this.value = value;
            this.disposable = File.OpenRead(string.Empty);
        }

        public int Value => this.value;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AssignIgnoredReturnValueToFieldInCtorWhenUnderscore()
            {
                var before = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private readonly int _value;

        public C(int value)
        {
            _value = value;
            ↓File.OpenRead(string.Empty);
        }

        public int Value => _value;
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private readonly int _value;
        private readonly System.IDisposable _disposable;

        public C(int value)
        {
            _value = value;
            _disposable = File.OpenRead(string.Empty);
        }

        public int Value => _value;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
