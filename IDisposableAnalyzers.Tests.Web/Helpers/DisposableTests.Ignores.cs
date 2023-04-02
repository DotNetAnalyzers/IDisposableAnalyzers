namespace IDisposableAnalyzers.Tests.Web.Helpers;

using System.Threading;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

public static partial class DisposableTests
{
    public static class Ignores
    {
        [Test]
        public static void HostBuildRun()
        {
            var code = @"
namespace N
{
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args).Build().Run();
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression("Host.CreateDefaultBuilder(args).Build()");
            Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
        }

        [Test]
        public static void HostBuildRunAsync()
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args).Build().RunAsync();
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression("Host.CreateDefaultBuilder(args).Build()");
            Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
        }
    }
}
