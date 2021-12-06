namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class AssignedValueWalkerTests
    {
        [TestCase("var temp1 = this.value;", "")]
        [TestCase("var temp2 = this.value;", "1")]
        [TestCase("var temp3 = this.value;", "1")]
        public static void LambdaInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;

    internal class C
    {
        private int value;

        public C()
        {
            var temp1 = this.value;
            this.Meh += (o, e) => this.value = 1;
            var temp2 = this.value;
        }

        public event EventHandler Meh;

        internal void M()
        {
            var temp3 = this.value;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void GenericOut()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        public T M<T>(out T t1)
        {
            return M(0, out t1);
        }

        public T M<T>(int _, out T t2)
        {
            t2 = default;
            return default;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var argument = syntaxTree.FindArgument("t1");
            using var walker = AssignedValueWalker.Borrow(argument.Expression, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual("default", actual);
        }
    }
}
