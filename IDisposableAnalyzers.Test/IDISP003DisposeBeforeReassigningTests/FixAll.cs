namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class FixAll
    {
        private static readonly AssignmentAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP003DisposeBeforeReassigning);
        private static readonly DisposeBeforeAssignFix Fix = new();

        [Test]
        public static void NotDisposingVariable()
        {
            var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var after = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void NotDisposingVariables()
        {
            var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream1 = File.OpenRead(string.Empty);
            var stream2 = File.OpenRead(string.Empty);
            ↓stream1 = File.OpenRead(string.Empty);
            ↓stream2 = File.OpenRead(string.Empty);
        }
    }
}";

            var after = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream1 = File.OpenRead(string.Empty);
            var stream2 = File.OpenRead(string.Empty);
            stream1?.Dispose();
            stream1 = File.OpenRead(string.Empty);
            stream2?.Dispose();
            stream2 = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
