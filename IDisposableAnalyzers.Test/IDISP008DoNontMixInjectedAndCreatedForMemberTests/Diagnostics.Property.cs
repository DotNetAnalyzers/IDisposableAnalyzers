namespace IDisposableAnalyzers.Test.IDISP008DoNontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Property
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [TestCase("arg ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? arg")]
            [TestCase("true ? arg : File.OpenRead(string.Empty)")]
            [TestCase("true ? File.OpenRead(string.Empty) : arg")]
            public static void InjectedAndCreated(string code)
            {
                var testCode = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void InjectedAndCreatedCtorAndInitializer()
            {
                var testCode = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void InjectedAndCreatedTwoCtors()
            {
                var testCode = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("public Stream Stream { get; protected set; }")]
            [TestCase("public Stream Stream { get; set; }")]
            [TestCase("protected Stream Stream { get; set; }")]
            public static void Mutable(string property)
            {
                var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);
    }
}".AssertReplace("public Stream Stream { get; set; }", property);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("internal Stream Stream { get; set; }")]
            [TestCase("public Stream Stream { get; set; }")]
            [TestCase("public Stream Stream { get; internal set; }")]
            public static void MutablePropertyInSealed(string property)
            {
                var testCode = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void InjectedAndCreatedInFactory()
            {
                var testCode = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, testCode);
            }
        }
    }
}
