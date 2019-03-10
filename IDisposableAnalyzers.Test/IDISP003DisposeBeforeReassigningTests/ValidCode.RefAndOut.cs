namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void OutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, IDISP003DisposeBeforeReassigning.Descriptor, testCode);
        }

        [TestCase("out _")]
        [TestCase("out var temp")]
        [TestCase("out var _")]
        [TestCase("out FileStream temp")]
        [TestCase("out FileStream _")]
        public void DictionaryTryGetValue(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public static class C
    {
        private static readonly Dictionary<int, FileStream> Map = new Dictionary<int, FileStream>();

        public static bool M(int i) => Map.TryGetValue(i, out _);
    }
}".AssertReplace("out _", expression);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("out _")]
        [TestCase("out var temp")]
        [TestCase("out var _")]
        [TestCase("out FileStream temp")]
        [TestCase("out FileStream _")]
        public void CallWithOutParameter(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public static class C
    {
        public static bool M(int i) => TryGet(i, out _);

        private static bool TryGet(int i, out FileStream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}".AssertReplace("out _", expression);

            AnalyzerAssert.Valid(Analyzer, IDISP003DisposeBeforeReassigning.Descriptor, testCode);
        }

        [TestCase("out _")]
        [TestCase("out var temp")]
        [TestCase("out var _")]
        [TestCase("out FileStream temp")]
        [TestCase("out FileStream _")]
        public void CachedInlineOutParameter(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public static class C
    {
        public static readonly Dictionary<int, FileStream> Map = new Dictionary<int, FileStream>();

        public static bool M(int i) => TryGet(i, out _);

        private static bool TryGet(int i, out FileStream stream)
        {
            if (Map.TryGetValue(i, out stream))
            {
                return true;
            }

            stream = File.OpenRead(string.Empty);
            Map.Add(i, stream);
            return true;
        }
    }
}".AssertReplace("out _", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CachedOutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public static class C
    {
        public static readonly Dictionary<int, FileStream> Map = new Dictionary<int, FileStream>();

        public static bool M(int i)
        {
            FileStream stream;
            return TryGet(i, out stream);
        }

        private static bool TryGet(int i, out FileStream stream)
        {
            if (Map.TryGetValue(i, out stream))
            {
                return true;
            }

            stream = File.OpenRead(string.Empty);
            Map.Add(i, stream);
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningVariableViaOutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public bool Update()
        {
            Stream stream;
            if (this.TryGetStream(out stream))
            {
                stream.Dispose();
                return true;
            }

            return false;
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
        public void AssigningOutParameterExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M(out IDisposable disposable) => disposable = File.OpenRead(string.Empty);
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

    public class C
    {
        public void M()
        {
            Stream stream;
            TryGetStream(out stream);
            stream?.Dispose();
            TryGetStream(out stream);
            stream.Dispose();
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

    public class C
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

    public class C
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

    public class C
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

        [Test]
        public void AssigningVariableViaRefParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public void M()
        {
            Stream stream = null;
            this.Assign(ref stream);
            stream.Dispose();
        }

        private void Assign(ref Stream result)
        {
            result = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningVariableViaRefParameterTwiceDisposingBetweenCalls()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M()
        {
            Stream stream = null;
            Assign(ref stream);
            stream?.Dispose();
            Assign(ref stream);
            stream.Dispose();
        }

        private void Assign(ref Stream result)
        {
            result = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ChainedOut()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
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

        [Test]
        public void SeparateDeclarationAndCreation()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void TryGetOutVar()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C()
        {
            if (TryGetStream(out var stream))
            {
                stream.Dispose();
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssigningReturnOut()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    class C
    {
        private static bool TryGetStream(string fileName, out Stream result)
        {
            if (File.Exists(fileName))
            {
                result = File.OpenRead(fileName);
                return true;
            }

            result = null;
            return false;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssigningReturnOutTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    class C
    {
        private static bool TryGetStream(string fileName, out Stream result)
        {
            if (File.Exists(fileName))
            {
                result = File.OpenRead(fileName);
                return true;
            }

            if (File.Exists(fileName))
            {
                result = File.OpenRead(fileName);
                return true;
            }

            result = null;
            return false;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }
    }
}
