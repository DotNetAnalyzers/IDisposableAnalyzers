namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class RefAndOut
        {
            [Test]
            public void LocalAssignedWithOutParameterSimple()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        Assign(out value, 1);
        var temp = value;
    }

    internal void Assign(out int outValue, int arg)
    {
        outValue = arg;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause("var temp = value").Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
#pragma warning disable GU0006 // Use nameof.
                    Assert.AreEqual("value", actual);
#pragma warning restore GU0006 // Use nameof.
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "value")]
            [TestCase("var temp3 = value;", "")]
            [TestCase("var temp4 = value;", "value")]
            public void LocalAssignedWithOutParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp1 = value;
        Assign(out value, 1);
        var temp2 = value;
    }

    internal void Bar()
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
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [Test]
            public void LocalAssignedWithOutParameterGeneric()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
{
    internal Foo()
    {
        T value;
        Assign(out value);
        var temp = value;
    }

    internal void Assign(out T outValue)
    {
        outValue = default(T);
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
#pragma warning disable GU0006 // Use nameof.
                    Assert.AreEqual("value", actual);
#pragma warning restore GU0006 // Use nameof.
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "value")]
            public void LocalAssignedWithChainedOutParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            int value;
            var temp1 = value;
            Assign1(out value, 1);
            var temp2 = value;
        }

        internal void Assign1(out int value1, int arg1)
        {
            Assign2(out value1, arg1);
        }

        internal void Assign2(out int value2, int arg2)
        {
            value2 = arg2;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "1")]
            public void LocalAssignedWithChainedRefParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
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
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = value", "")]
            [TestCase("var temp2 = value", "1")]
            public void LocalAssignedWithRefParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
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
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, this.value")]
            [TestCase("var temp3 = this.value;", "1, this.value, this.value")]
            [TestCase("var temp4 = this.value;", "1, this.value, this.value")]
            public void FieldAssignedWithOutParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int value = 1;

        public Foo()
        {
            var temp1 = this.value;
            this.Assign(out this.value, 2);
            var temp2 = this.value;
        }

        internal void Bar()
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
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2")]
            public void FieldAssignedWithRefParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int value = 1;

        public Foo()
        {
            var temp1 = this.value;
            this.Assign(ref this.value);
            var temp2 = this.value;
        }

        internal void Bar()
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
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2, 3")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            public void FieldAssignedWithRefParameterArgument(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int value = 1;

        public Foo()
        {
            var temp1 = this.value;
            this.Assign(ref this.value, 2);
            var temp2 = this.value;
        }

        internal void Bar()
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
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }
        }
    }
}