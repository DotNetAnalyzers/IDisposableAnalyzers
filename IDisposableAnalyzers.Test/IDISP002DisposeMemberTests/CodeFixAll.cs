namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFixAll
    {
        [Test]
        public void NotDisposingFieldAssignedInCtor()
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
            this.stream = File.OpenRead(string.Empty);
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
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        [Explicit("Order is random due to async.")]
        public void NotDisposingFieldsAssignedInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream1;
        ↓private readonly Stream stream2;

        public Foo()
        {
            this.stream1 = File.OpenRead(string.Empty);
            this.stream2 = File.OpenRead(string.Empty);
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
        private readonly Stream stream1;
        private readonly Stream stream2;

        public Foo()
        {
            this.stream1 = File.OpenRead(string.Empty);
            this.stream2 = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream1?.Dispose();
            this.stream2?.Dispose();
        }
    }
}";
            AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }
    }
}