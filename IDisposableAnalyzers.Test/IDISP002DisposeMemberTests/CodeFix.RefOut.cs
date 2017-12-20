namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class RefAndOut
        {
            private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly DisposeMemberCodeFixProvider CodeFix = new DisposeMemberCodeFixProvider();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP002");

            [Test]
            public void AssigningFieldViaOutParameterInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo()
        {
            if (TryGetStream(out this.stream))
            {
            }
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            if (TryGetStream(out this.stream))
            {
            }
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssigningFieldViaRefParameterInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo()
        {
            if (TryGetStream(ref this.stream))
            {
            }
        }

        public bool TryGetStream(ref Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            if (TryGetStream(ref this.stream))
            {
            }
        }

        public bool TryGetStream(ref Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
