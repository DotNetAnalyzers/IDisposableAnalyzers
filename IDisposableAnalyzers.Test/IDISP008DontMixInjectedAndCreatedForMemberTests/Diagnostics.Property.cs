namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class Diagnostics
    {
        internal class Property
        {
            private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP008");

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

    public sealed class Foo
    {
        private static readonly Stream StaticStream = File.OpenRead(string.Empty);

        public Foo(Stream arg)
        {
            this.Stream = arg ?? File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }
    }
}";
                testCode = testCode.AssertReplace("arg ?? File.OpenRead(string.Empty)", code);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void InjectedAndCreatedCtorAndInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Foo(Stream stream)
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

    public sealed class Foo
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Foo(Stream stream)
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

    public class Foo
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);
    }
}";
                testCode = testCode.AssertReplace("public Stream Stream { get; set; }", property);
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

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
                testCode = testCode.AssertReplace("public Stream Stream { get; set; }", property);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void InjectedAndCreatedInFactory()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo(IDisposable bar)
        {
            this.Bar = bar;
        }

        ↓public IDisposable Bar { get; }

        public static Foo Create() => new Foo(new Disposable());
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }
        }
    }
}
