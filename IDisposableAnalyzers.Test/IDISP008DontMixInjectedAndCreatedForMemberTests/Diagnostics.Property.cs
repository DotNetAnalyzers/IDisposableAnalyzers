namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class Property
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [TestCase("arg ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? arg")]
            [TestCase("true ? arg : File.OpenRead(string.Empty)")]
            [TestCase("true ? File.OpenRead(string.Empty) : arg")]
            public void InjectedAndCreated(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private static readonly Stream StaticStream = File.OpenRead(string.Empty);

        public C(Stream arg)
        {
            this.Stream = arg ?? File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }
    }
}".AssertReplace("arg ?? File.OpenRead(string.Empty)", code);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void InjectedAndCreatedCtorAndInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        public C(Stream stream)
        {
            this.Stream = stream;
        }

        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void InjectedAndCreatedTwoCtors()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public C(Stream stream)
        {
            this.Stream = stream;
        }

        ↓public Stream Stream { get; }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("public Stream Stream { get; protected set; }")]
            [TestCase("public Stream Stream { get; set; }")]
            [TestCase("protected Stream Stream { get; set; }")]
            public void Mutable(string property)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);
    }
}".AssertReplace("public Stream Stream { get; set; }", property);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("internal Stream Stream { get; set; }")]
            [TestCase("public Stream Stream { get; set; }")]
            [TestCase("public Stream Stream { get; internal set; }")]
            public void MutablePropertyInSealed(string property)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}".AssertReplace("public Stream Stream { get; set; }", property);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void InjectedAndCreatedInFactory()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public C(IDisposable bar)
        {
            this.M = bar;
        }

        ↓public IDisposable M { get; }

        public static C Create() => new C(new Disposable());
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }
        }
    }
}
