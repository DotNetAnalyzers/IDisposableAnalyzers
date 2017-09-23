namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class WhenDisposing
        {
            [Test]
            public void DisposingVariable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";

                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public void DisposeBeforeAssigningInIfElse(string dispose)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            if (true)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
                testCode = testCode.AssertReplace("stream.Dispose();", dispose);
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public void DisposeBeforeAssigningBeforeIfElse(string dispose)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            stream.Dispose();
            if (true)
            {
                stream = null;
            }
            else
            {
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
                testCode = testCode.AssertReplace("stream.Dispose();", dispose);
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public void DisposeFieldBeforeIfElseReassigning(string dispose)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void Meh()
        {
            this.stream.Dispose();
            if (true)
            {
                this.stream = null;
            }
            else
            {
                this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
                testCode = testCode.AssertReplace("stream.Dispose();", dispose);
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [Test]
            public void DisposingParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [Test]
            public void DisposingFieldInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        public Foo()
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [Test]
            public void DisposingFieldInMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [Test]
            public void ConditionallyDisposingFieldInMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [Test]
            public void ConditionallyDisposingUnderscoreFieldInMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream _stream;

        public void Meh()
        {
            _stream?.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }

            [Test]
            public void DisposingUnderscoreFieldInMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream _stream;

        public void Meh()
        {
            _stream.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP003DisposeBeforeReassigning>(testCode);
            }
        }
    }
}