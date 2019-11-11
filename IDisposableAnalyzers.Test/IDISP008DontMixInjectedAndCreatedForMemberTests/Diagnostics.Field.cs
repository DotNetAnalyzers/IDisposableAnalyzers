namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Field
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [TestCase("arg ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? arg")]
            [TestCase("true ? arg : File.OpenRead(string.Empty)")]
            [TestCase("true ? File.OpenRead(string.Empty) : arg")]
            [TestCase("Stream ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? Stream")]
            [TestCase("true ? Stream : File.OpenRead(string.Empty)")]
            public static void InjectedAndCreated(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        ↓private readonly Stream stream;

        public C(Stream arg)
        {
            this.stream = arg ?? File.OpenRead(string.Empty);
        }
    }
}".AssertReplace("arg ?? File.OpenRead(string.Empty)", code);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("public Stream Stream")]
            [TestCase("internal Stream Stream")]
            public static void MutableFieldInSealed(string property)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓public Stream Stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}".AssertReplace("public Stream Stream", property);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void InjectedAndCreatedCtorAndInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public C(Stream stream)
        {
            this.stream = stream;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void InjectedAndCreatedTwoCtors()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        ↓private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public C(Stream stream)
        {
            this.stream = stream;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void ProtectedMutable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        ↓protected Stream stream = File.OpenRead(string.Empty);
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void BackingFieldAssignedWithCreatedAndPropertyWithInjected()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        ↓private Stream stream = File.OpenRead(string.Empty);

        public C(Stream arg)
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void BackingFieldAssignedWithInjectedAndPropertyWithCreated()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        ↓private Stream stream;

        public C(Stream arg)
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void InjectedAndCreatedViaFactory()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        ↓private readonly IDisposable bar;

        public C(IDisposable bar)
        {
            this.bar = bar;
        }

        public static C Create() => new C(new Disposable());
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, testCode);
            }
        }
    }
}
