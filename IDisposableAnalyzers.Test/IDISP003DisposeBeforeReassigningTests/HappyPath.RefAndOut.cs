namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(IDISP003DisposeBeforeReassigning))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    internal class HappyPathRefAndOut<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();

#pragma warning disable SA1203 // Constants must appear before fields
        //// ReSharper disable once UnusedMember.Local
        private const string DisposableCode = @"
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

        [Explicit("Temporary")]
        [Test]
        public void AssigningVariableViaOutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public bool Update()
        {
            Stream stream;
            return TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningVariableViaOutParameterTwiceDisposingBetweenCalls()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream;
            TryGetStream(out stream);
            stream?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningFieldViaConcurrentDictionaryTryGetValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        private Stream current;

        public bool Update(int number)
        {
            return this.Cache.TryGetValue(number, out this.current);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningFieldViaConcurrentDictionaryTryGetValueTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        private Stream current;

        public bool Update(int number)
        {
            return this.Cache.TryGetValue(number, out this.current);
            return this.Cache.TryGetValue(number + 1, out this.current);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningFieldWithCachedViaOutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public bool Update()
        {
            return TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream result)
        {
            result = this.stream;
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Explicit("Temporary")]
        [Test]
        public void AssigningVariableViaRefParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream = null;
            Assign(ref stream);
        }

        public void Assign(ref Stream result)
        {
            result = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Explicit("Temporary")]
        [Test]
        public void AssigningVariableViaRefParameterTwiceDisposingBetweenCalls()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream = null;
            Assign(ref stream);
            stream?.Dispose();
            Assign(ref stream);
        }

        public void Assign(ref Stream result)
        {
            result = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Explicit("Temporary")]
        [Test]
        public void ChainedOut()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public static bool TryGetStream(out Stream stream)
        {
            return TryGetStreamCore(out stream);
        }

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
