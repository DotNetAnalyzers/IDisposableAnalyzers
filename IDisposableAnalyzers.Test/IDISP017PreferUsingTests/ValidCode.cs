namespace IDisposableAnalyzers.Test.IDISP017PreferUsingTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();

        [Test]
        public void DisposingField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Stream Calculated => this.stream;

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingArrayItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable[] disposables;

        public void Bar()
        {
            var disposable = disposables[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingDictionaryItem()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo : IDisposable
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public void Bar()
        {
            var disposable = map[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReassignAfterDispose()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            b = stream.ReadByte();
            stream.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReassignViaOutAfterDispose()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            Stream stream;
            Create(out stream);
            var b = stream.ReadByte();
            stream.Dispose();
            Create(out stream);
            b = stream.ReadByte();
            stream.Dispose();
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
