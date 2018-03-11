namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class Diagnostics
    {
        internal class Field
        {
            private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [TestCase("arg ?? File.OpenRead(string.Empty)")]
            [TestCase("arg ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? arg")]
            [TestCase("File.OpenRead(string.Empty) ?? arg")]
            [TestCase("true ? arg : File.OpenRead(string.Empty)")]
            [TestCase("true ? arg : File.OpenRead(string.Empty)")]
            [TestCase("true ? File.OpenRead(string.Empty) : arg")]
            [TestCase("true ? File.OpenRead(string.Empty) : arg")]
            public void InjectedAndCreated(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        ↓private readonly Stream stream;

        public Foo(Stream arg)
        {
            this.stream = arg ?? File.OpenRead(string.Empty);
        }
    }
}";
                testCode = testCode.AssertReplace("arg ?? File.OpenRead(string.Empty)", code);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("public Stream Stream")]
            [TestCase("internal Stream Stream")]
            public void MutableFieldInSealed(string property)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
                testCode = testCode.AssertReplace("public Stream Stream", property);
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
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo(Stream stream)
        {
            this.stream = stream;
        }
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
        ↓private readonly Stream stream;

        public Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public Foo(Stream stream)
        {
            this.stream = stream;
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void ProtectedMutable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓protected Stream stream = File.OpenRead(string.Empty);
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void BackingFieldAssignedWithCreatedAndPropertyWithInjected()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private Stream stream = File.OpenRead(string.Empty);

        public Foo(Stream arg)
        {
            this.Stream = arg;
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void BackingFieldAssignedWithInjectedAndPropertyWithCreated()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private Stream stream;

        public Foo(Stream arg)
        {
            this.stream = arg;
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void InjectedAndCreatedViaFactory()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        ↓private readonly IDisposable bar;

        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }

        public static Foo Create() => new Foo(new Disposable());
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }
        }
    }
}
