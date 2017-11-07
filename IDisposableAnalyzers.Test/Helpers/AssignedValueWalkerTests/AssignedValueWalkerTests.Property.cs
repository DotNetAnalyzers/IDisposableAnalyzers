namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        [TestCase("var temp1 = this.Bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "1, 2")]
        [TestCase("var temp3 = this.Bar;", "1, 2")]
        public void AutoPropertyGetSetAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    public Foo()
    {
        var temp1 = this.Bar;
        this.Bar = 2;
        var temp2 = this.Bar;
    }

    public int Bar { get; set; } = 1;

    public void Meh()
    {
        var temp3 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.Bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "1, 2")]
        [TestCase("var temp3 = this.Bar;", "1, 2")]
        public void AutoPropertyGetOnlyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    public Foo()
    {
        var temp1 = this.Bar;
        this.Bar = 2;
        var temp2 = this.Bar;
    }

    public int Bar { get; } = 1;

    public void Meh()
    {
        var temp3 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "")]
        [TestCase("var temp5 = this.bar;", "1, 2")]
        [TestCase("var temp6 = this.Bar;", "")]
        public void BackingFieldPrivateSetInitializedAndAssignedInCtor(string code1, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        var temp1 = this.bar;
        var temp2 = this.Bar;
        this.bar = 2;
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code1).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "")]
        [TestCase("var temp5 = this.bar;", "1, 2, value")]
        [TestCase("var temp6 = this.Bar;", "")]
        public void BackingFieldPublicSetInitializedAndAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private int bar = 1;

        public Foo()
        {
            var temp1 = this.bar;
            var temp2 = this.Bar;
            this.bar = 2;
            var temp3 = this.bar;
            var temp4 = this.Bar;
        }

        public int Bar
        {
            get { return this.bar; }
            set { this.bar = value; }
        }

        public void Meh()
        {
            var temp5 = this.bar;
            var temp6 = this.Bar;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void BackingFieldPublicSetSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private int bar;

        public int Bar
        {
            get { return this.bar; }
            set { this.bar = value; }
        }

        public void Meh()
        {
            var temp = this.bar;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = this.bar").Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
#pragma warning disable GU0006 // Use nameof.
                Assert.AreEqual("value", actual);
#pragma warning restore GU0006 // Use nameof.
            }
        }

        [Test]
        public void BackingFieldPrivateSetSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private int bar;

        public int Bar
        {
            get { return this.bar; }
            private set { this.bar = value; }
        }

        public void Meh()
        {
            var temp = this.bar;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = this.bar").Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(string.Empty, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "2")]
        [TestCase("var temp5 = this.bar;", "1, 2")]
        [TestCase("var temp6 = this.Bar;", "2")]
        public void BackingFieldPrivateSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        var temp1 = this.bar;
        var temp2 = this.Bar;
        this.Bar = 2;
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "2")]
        [TestCase("var temp5 = this.bar;", "1, 2, value")]
        [TestCase("var temp6 = this.Bar;", "2")]
        public void BackingFieldPublicSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        var temp1 = this.bar;
        var temp2 = this.Bar;
        this.Bar = 2;
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2, value / 2, 3")]
        [TestCase("var temp4 = this.Bar;", "2")]
        [TestCase("var temp5 = this.bar;", "1, 2, value / 2, 3, value, value")]
        [TestCase("var temp6 = this.Bar;", "2")]
        public void BackingFieldPublicSetInitializedAndPropertyAssignedInCtorWeirdSetter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private int bar = 1;

        public Foo()
        {
            var temp1 = this.bar;
            var temp2 = this.Bar;
            this.Bar = 2;
            var temp3 = this.bar;
            var temp4 = this.Bar;
        }

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (true)
                {
                    this.bar = value;
                }
                else
                {
                    this.bar = value;
                }

                this.bar = value / 2;
                this.bar = 3;
            }
        }

        public void Meh()
        {
            var temp5 = this.bar;
            var temp6 = this.Bar;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.Bar;", "")]
        [TestCase("var temp2 = this.Bar;", "2")]
        [TestCase("var temp3 = this.Bar;", "2, value")]
        public void RecursiveGetAndSet(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo()
        {
            var temp1 = this.Bar;
            this.Bar = 2;
            var temp2 = this.Bar;
        }

        public int Bar
        {
            get { return this.Bar; }
            set { this.Bar = value; }
        }

        public void Meh()
        {
            var temp3 = this.Bar;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}