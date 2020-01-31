namespace IDisposableAnalyzers.NetCoreTests.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(LocalDeclarationAnalyzer))]
    [TestFixture(typeof(ArgumentAnalyzer))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    public static class Valid<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly DiagnosticAnalyzer Analyzer = new T();

        [Test]
        public static void LocalDisposeAsync()
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public async ValueTask M()
        {
            var x = File.OpenRead(string.Empty);
            await x.DisposeAsync();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalDisposeAsyncInFinally()
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public async ValueTask M()
        {
            var x = File.OpenRead(string.Empty);
            try
            {

            }
            finally
            {
                await x.DisposeAsync();
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
