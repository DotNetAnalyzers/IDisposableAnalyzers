namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddUsingOutParameter
        {
            private static readonly ArgumentAnalyzer Analyzer = new();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);
            private static readonly AddUsingFix Fix = new();

            [Test]
            public static void OutParameter()
            {
                var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C()
        {
            Stream stream;
            if (TryGetStream(↓out stream))
            {
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C()
        {
            Stream stream;
            if (TryGetStream(out stream))
            {
                using (stream)
                {
                }
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void OutVarParameter()
            {
                var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C()
        {
            if (TryGetStream(↓out var stream))
            {
            }
        }

        private static bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C()
        {
            if (TryGetStream(out var stream))
            {
                using (stream)
                {
                }
            }
        }

        private static bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
