namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class CreateAndAssignField
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP004DontIgnoreCreated.Descriptor);
            private static readonly CodeFixProvider Fix = new CreateAndAssignFieldFix();

            [Test]
            public void AssignIgnoredReturnValueToFieldInCtorWhenEmpty()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssignIgnoredReturnValueToFieldInCtorWhenUsesThis()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssignIgnoredReturnValueToFieldInCtorWhenUnderscore()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
