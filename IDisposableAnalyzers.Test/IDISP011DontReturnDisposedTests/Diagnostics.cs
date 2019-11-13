namespace IDisposableAnalyzers.Test.IDISP011DontReturnDisposedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP011");

        [Test]
        public static void ReturnFileOpenReadFromUsing()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public object Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return ↓stream;
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnFileOpenReadDisposed()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public object Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            return ↓stream;
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnLazyFromUsing()
        {
            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        public IEnumerable<string> F()
        {
            using(var reader = File.OpenText(string.Empty))
                return Use(↓reader);
        }

        IEnumerable<string> Use(TextReader reader)
        {
            string line;
            while((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnLazyFromUsingNested()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        public IEnumerable<string> F()
        {
            using (var reader = File.OpenText(string.Empty))
                return Use(↓reader);
        }

        private IEnumerable<string> Use(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException();
            }

            return UseCore(reader);
        }

        private IEnumerable<string> UseCore(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
