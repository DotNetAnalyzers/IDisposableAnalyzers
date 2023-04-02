namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests;

using System.Linq;
using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

public static partial class AssignedValueWalkerTests
{
    public static class RefAndOut
    {
        [Test]
        public static void LocalAssignedWithOutParameterSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value;
            Assign(out value, 1);
            var temp = value;
        }

        internal void Assign(out int outValue, int arg)
        {
            outValue = arg;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("1", walker.Values.Single().ToString());
        }

        [TestCase("out _")]
        [TestCase("out var _")]
        [TestCase("out var temp")]
        [TestCase("out int temp")]
        public static void DiscardedOut(string expression)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public static class C
    {
        public static bool M() => TryGet(out _);

        private static bool TryGet(out int i)
        {
            i = 1;
            return true;
        }
    }
}".AssertReplace("out _", expression));
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindArgument(expression).Expression;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("1", string.Join(", ", walker.Values));
        }

        [TestCase("out _")]
        [TestCase("out var _")]
        [TestCase("out var temp")]
        [TestCase("out int temp")]
        public static void DiscardedOutAssignedWithArgument(string expression)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public static class C
    {
        public static bool M() => TryGet(1, out _);

        private static bool TryGet(int arg, out int i)
        {
            i = arg;
            return true;
        }
    }
}".AssertReplace("out _", expression));
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindArgument(expression).Expression;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            CollectionAssert.AreEqual("1", string.Join(", ", walker.Values));
        }

        [TestCase("out _")]
        [TestCase("out var _")]
        [TestCase("out var temp")]
        [TestCase("out int temp")]
        public static void DiscardedOutCachedAndAssigned(string expression)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    public static class C
    {
        public static readonly Dictionary<int, int> Map = new Dictionary<int, int>();

        public static bool M(int i) => TryGet(i, out _);

        private static bool TryGet(int i, out int result)
        {
            if (Map.TryGetValue(i, out result))
            {
                return true;
            }

            result = 1;
            Map.Add(i, result);
            return true;
        }
    }
}".AssertReplace("out _", expression));
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindArgument(expression).Expression;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("result, 1", string.Join(", ", walker.Values));
        }

        [Test]
        public static void OutParameterCachedAndAssigned()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Collections.Generic;

    public static class C
    {
        public static readonly Dictionary<int, int> Map = new Dictionary<int, int>();

        private static bool TryGet(int i, out int result)
        {
            if (Map.TryGetValue(i, out result))
            {
                return true;
            }

            result = 1;
            Map.Add(i, result);
            return true;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("result");
            Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var parameter));
            using var walker = AssignedValueWalker.Borrow(parameter, semanticModel, CancellationToken.None);
            Assert.AreEqual("result, 1", string.Join(", ", walker.Values));
        }

        [Test]
        public static void LocalAssignedWithOutParameterOtherClass()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    class C
    {
        C(M bar)
        {
            int value;
            bar.Assign(out value, 1);
            var temp = value;
        }
    }

    class M
    {
        internal void Assign(out int outValue, int arg)
        {
            outValue = arg;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("1", walker.Values.Single().ToString());
        }

        [Test]
        public static void LocalAssignedWithOutParameterOtherClassElvis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    class C
    {
        C(M bar)
        {
            int value;
            bar?.Assign(out value, 1);
            var temp = value;
        }
    }

    class M
    {
        internal void Assign(out int outValue, int arg)
        {
            outValue = arg;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("1", walker.Values.Single().ToString());
        }

        [TestCase("var temp1 = value;", "")]
        [TestCase("var temp2 = value;", "1")]
        [TestCase("var temp3 = value;", "")]
        [TestCase("var temp4 = value;", "2")]
        public static void LocalAssignedWithOutParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value;
            var temp1 = value;
            Assign(out value, 1);
            var temp2 = value;
        }

        internal void M()
        {
            int value;
            var temp3 = value;
            Assign(out value, 2);
            var temp4 = value;
        }

        internal void Assign(out int outValue, int arg)
        {
            outValue = arg;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [Test]
        public static void LocalAssignedWithOutParameterGeneric()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C<T>
    {
        internal C()
        {
            T value;
            Assign(out value);
            var temp = value;
        }

        internal void Assign(out T outValue)
        {
            outValue = default(T);
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value;").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("default(T)", walker.Values.Single().ToString());
        }

        [TestCase("var temp1 = value;", "0")]
        [TestCase("var temp2 = value;", "0, 1")]
        public static void LocalAssignedWithChainedOutParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value = 0;
            var temp1 = value;
            Assign1(out value, 1);
            var temp2 = value;
        }

        internal static void Assign1(out int value1, int arg1)
        {
            Assign2(out value1, arg1);
        }

        internal static void Assign2(out int value2, int arg2)
        {
            value2 = arg2;
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

        [TestCase("var temp1 = value;", "")]
        [TestCase("var temp2 = value;", "1")]
        public static void LocalAssignedWithChainedRefParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value;
            var temp1 = value;
            Assign1(ref value);
            var temp2 = value;
        }

        internal void Assign1(ref int value1)
        {
             Assign2(ref value1);
        }

        internal void Assign2(ref int value2)
        {
            value2 = 1;
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

        [TestCase("var temp1 = value", "")]
        [TestCase("var temp2 = value", "1")]
        public static void LocalAssignedWithRefParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value;
            var temp1 = value;
            Assign(ref value);
            var temp2 = value;
        }

        internal void Assign(ref int value)
        {
            value = 1;
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

        [TestCase("var temp1 = this.value;", "1")]
        [TestCase("var temp2 = this.value;", "1, 2")]
        [TestCase("var temp3 = this.value;", "1, 2, 3")]
        [TestCase("var temp4 = this.value;", "1, 2, 3")]
        public static void FieldAssignedWithOutParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private int value = 1;

        public C()
        {
            var temp1 = this.value;
            this.Assign(out this.value, 2);
            var temp2 = this.value;
        }

        internal void M()
        {
            var temp3 = this.value;
            this.Assign(out this.value, 3);
            var temp4 = this.value;
        }

        private void Assign(out int outValue, int arg)
        {
            outValue = arg;
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

        [TestCase("var temp1 = this.value;", "1")]
        [TestCase("var temp2 = this.value;", "1, 2")]
        [TestCase("var temp3 = this.value;", "1, 2")]
        [TestCase("var temp4 = this.value;", "1, 2")]
        public static void FieldAssignedWithRefParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private int value = 1;

        public C()
        {
            var temp1 = this.value;
            this.Assign(ref this.value);
            var temp2 = this.value;
        }

        internal void M()
        {
            var temp3 = this.value;
            this.Assign(ref this.value);
            var temp4 = this.value;
        }

        private void Assign(ref int refValue)
        {
            refValue = 2;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("var temp1 = this.value;", "1")]
        [TestCase("var temp2 = this.value;", "1, 2")]
        [TestCase("var temp3 = this.value;", "1, 2, 3")]
        [TestCase("var temp4 = this.value;", "1, 2, 3")]
        public static void FieldAssignedWithRefParameterArgument(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private int value = 1;

        public C()
        {
            var temp1 = this.value;
            this.Assign(ref this.value, 2);
            var temp2 = this.value;
        }

        internal void M()
        {
            var temp3 = this.value;
            this.Assign(ref this.value, 3);
            var temp4 = this.value;
        }

        private void Assign(ref int refValue, int arg)
        {
            refValue = arg;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [Test]
        public static void RefBeforeOut()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value = 0;
            Assign(ref value, out value);
            var temp = value;
        }

        internal void Assign(ref int refValue, out int outValue)
        {
            outValue = 2;
            refValue = 1;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("0, 1, 2", string.Join(", ", walker.Values));
        }

        [Test]
        public static void RecursiveOutAssigned()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value;
            M(out value);
            var temp = value;
        }

        private static bool M(out int result)
        {
            result = 0;
            if (result < 0)
            {
                return M(out result);
            }

            result = 1;
            return true;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("0, result, 1", string.Join(", ", walker.Values));
        }

        [Test]
        public static void RecursiveOut()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal void M()
        {
            int value;
            M(1.0, out value);
            var temp = value;
        }

        private static bool M(double foo, out int result)
        {
            result = 0;
            return M(3.0, out result);
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = value").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            Assert.AreEqual("0, result", string.Join(", ", walker.Values));
        }
    }
}
