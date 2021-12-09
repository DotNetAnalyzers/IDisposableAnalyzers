namespace IDisposableAnalyzers.Test.IDISP017PreferUsingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DisposeCallAnalyzer Analyzer = new();

        [Test]
        public static void DisposingField()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingArrayItem()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable[] disposables = new IDisposable[1];

        public void M()
        {
            var disposable = disposables[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingDictionaryItem()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public sealed class C : IDisposable
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public void M()
        {
            var disposable = map[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignAfterDispose()
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignViaOutAfterDispose()
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
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
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
