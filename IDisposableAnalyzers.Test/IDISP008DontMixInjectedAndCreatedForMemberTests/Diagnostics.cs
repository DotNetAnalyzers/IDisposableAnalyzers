namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class Diagnostics
    {
        private static readonly string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        [TestCase("arg ?? File.OpenRead(string.Empty)")]
        [TestCase("arg ?? File.OpenRead(string.Empty)")]
        [TestCase("File.OpenRead(string.Empty) ?? arg")]
        [TestCase("File.OpenRead(string.Empty) ?? arg")]
        [TestCase("true ? arg : File.OpenRead(string.Empty)")]
        [TestCase("true ? arg : File.OpenRead(string.Empty)")]
        [TestCase("true ? File.OpenRead(string.Empty) : arg")]
        [TestCase("true ? File.OpenRead(string.Empty) : arg")]
        public void InjectedAndCreatedField(string code)
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [Test]
        public void InjectedAndCreatedFieldCtorAndInitializer()
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [Test]
        public void InjectedAndCreatedFieldTwoCtors()
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; set; }")]
        public void InjectedAndCreatedProperty(string property)
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
            testCode = testCode.AssertReplace("public Stream Stream { get; }", property);
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [Test]
        public void InjectedAndCreatedPropertyTwoCtors()
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [Test]
        public void ProtectedMutableField()
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [Test]
        public void ProtectedMutableProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓public Stream Stream { get; protected set; } = File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
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
            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }

        [Test]
        public void PublicMethodRefParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public bool TryGetStream(ref Stream stream)
        {
            ↓stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            AnalyzerAssert.Diagnostics<IDISP008DontMixInjectedAndCreatedForMember>(testCode);
        }
    }
}