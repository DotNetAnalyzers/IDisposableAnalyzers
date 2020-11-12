namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class AssignedValueWalkerTests
    {
        public static class Indexer
        {
            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedArrayIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class C
{
    internal C()
    {
        var ints = new int[] { 1, 2 };
        var temp1 = ints[0];
        ints[0] = 3;
        var temp2 = ints[0];
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedTypedArrayIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class C
{
    internal C()
    {
        int[] ints = { 1, 2 };
        var temp1 = ints[0];
        ints[0] = 3;
        var temp2 = ints[0];
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedListOfIntIndexerAfterSetItem(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    internal class C
    {
        internal C()
        {
            var ints = new List<int> { 1, 2 };
            var temp1 = ints[0];
            ints[0] = 3;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedListOfIntIndexerAfterAddItem(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    internal class C
    {
        internal C()
        {
            var ints = new List<int> { 1, 2 };
            var temp1 = ints[0];
            ints.Add(3);
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedElementStyleDictionaryIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    internal class C
    {
        internal C()
        {
            var ints = new Dictionary<int, int> 
            { 
                [1] = 1,
                [2] = 2,
            };
            var temp1 = ints[0];
            ints[3] = 3;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedDictionaryIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    internal class C
    {
        internal C()
        {
            var ints = new Dictionary<int, int> 
            {
                { 1, 1 }, 
                { 2, 2 }, 
            };
            var temp1 = ints[0];
            ints[3] = 3;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public static void InitializedDictionaryAfterAdd(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    internal class C
    {
        internal C()
        {
            var ints = new Dictionary<int, int> 
            {
                { 1, 1 }, 
                { 2, 2 }, 
            };
            var temp1 = ints[0];
            ints.Add(3, 3);
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", walker.Values);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
