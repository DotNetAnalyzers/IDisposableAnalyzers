namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class AssignedValueWalkerTests
    {
        [TestCase("var temp1 = this.M;", "1")]
        [TestCase("var temp2 = this.M;", "1, 2")]
        [TestCase("var temp3 = this.M;", "1, 2")]
        public static void AutoPropertyGetSetAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C()
        {
            var temp1 = this.M;
            this.M = 2;
            var temp2 = this.M;
        }

        public int M { get; set; } = 1;

        public void Meh()
        {
            var temp3 = this.M;
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

        [TestCase("var temp1 = this.M;", "1")]
        [TestCase("var temp2 = this.M;", "1, 2")]
        [TestCase("var temp3 = this.M;", "1, 2")]
        public static void AutoPropertyGetOnlyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C()
        {
            var temp1 = this.M;
            this.M = 2;
            var temp2 = this.M;
        }

        public int M { get; } = 1;

        public void Meh()
        {
            var temp3 = this.M;
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

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.M;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.M;", "")]
        [TestCase("var temp5 = this.bar;", "1, 2")]
        [TestCase("var temp6 = this.M;", "")]
        public static void BackingFieldPrivateSetInitializedAndAssignedInCtor(string code1, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int bar = 1;

        public C()
        {
            var temp1 = this.bar;
            var temp2 = this.M;
            this.bar = 2;
            var temp3 = this.bar;
            var temp4 = this.M;
        }

        public int M
        {
            get { return this.bar; }
            private set { this.bar = value; }
        }

        public void Meh()
        {
            var temp5 = this.bar;
            var temp6 = this.M;
        }
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
        [TestCase("var temp2 = this.M;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.M;", "")]
        [TestCase("var temp5 = this.bar;", "1, 2, value")]
        [TestCase("var temp6 = this.M;", "")]
        public static void BackingFieldPublicSetInitializedAndAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int bar = 1;

        public C()
        {
            var temp1 = this.bar;
            var temp2 = this.M;
            this.bar = 2;
            var temp3 = this.bar;
            var temp4 = this.M;
        }

        public int M
        {
            get { return this.bar; }
            set { this.bar = value; }
        }

        public void Meh()
        {
            var temp5 = this.bar;
            var temp6 = this.M;
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
        public static void BackingFieldPublicSetSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int bar;

        public int M
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
        public static void BackingFieldPrivateSetSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int bar;

        public int M
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
        [TestCase("var temp2 = this.M;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.M;", "2")]
        [TestCase("var temp5 = this.bar;", "1, 2")]
        [TestCase("var temp6 = this.M;", "2")]
        public static void BackingFieldPrivateSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class C
{
    private int bar = 1;

    public C()
    {
        var temp1 = this.bar;
        var temp2 = this.M;
        this.M = 2;
        var temp3 = this.bar;
        var temp4 = this.M;
    }

    public int M
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.M;
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
        [TestCase("var temp2 = this.M;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.M;", "2")]
        [TestCase("var temp5 = this.bar;", "1, 2, value")]
        [TestCase("var temp6 = this.M;", "2")]
        public static void BackingFieldPublicSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class C
{
    private int bar = 1;

    public C()
    {
        var temp1 = this.bar;
        var temp2 = this.M;
        this.M = 2;
        var temp3 = this.bar;
        var temp4 = this.M;
    }

    public int M
    {
        get { return this.bar; }
        set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.M;
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
        [TestCase("var temp2 = this.M;", "")]
        [TestCase("var temp3 = this.bar;", "1, 2, value / 2, 3")]
        [TestCase("var temp4 = this.M;", "2")]
        [TestCase("var temp5 = this.bar;", "1, 2, value / 2, 3, value, value")]
        [TestCase("var temp6 = this.M;", "2")]
        public static void BackingFieldPublicSetInitializedAndPropertyAssignedInCtorWeirdSetter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int bar = 1;

        public C()
        {
            var temp1 = this.bar;
            var temp2 = this.M;
            this.M = 2;
            var temp3 = this.bar;
            var temp4 = this.M;
        }

        public int M
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
            var temp6 = this.M;
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

        [TestCase("var temp1 = this.M;", "")]
        [TestCase("var temp2 = this.M;", "2")]
        [TestCase("var temp3 = this.M;", "2, value")]
        public static void RecursiveGetAndSet(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C()
        {
            var temp1 = this.M;
            this.M = 2;
            var temp2 = this.M;
        }

        public int M
        {
            get { return this.M; }
            set { this.M = value; }
        }

        public void Meh()
        {
            var temp3 = this.M;
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
        public static void Recursive()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class C
    {
        private C1 c1;
        private C1 c2;

        public C1 P1
        {
            get
            {
                return this.c1;
            }

            set
            {
                if (Equals(value, this.c1))
                {
                    return;
                }

                if (value != null && this.c2 != null)
                {
                    this.P2 = null;
                }

                if (this.c1 != null)
                {
                    this.c1.Selected = false;
                }

                this.c1 = value;
                if (this.c1 != null)
                {
                    this.c1.Selected = true;
                }
            }
        }

        public C1 P2
        {
            get
            {
                return this.c2;
            }

            set
            {
                if (Equals(value, this.c2))
                {
                    return;
                }

                if (value != null && this.c1 != null)
                {
                    this.P1 = null;
                }

                if (this.c2 != null)
                {
                    this.c2.Selected = false;
                }

                this.c2 = value;
                if (this.c2 != null)
                {
                    this.c2.Selected = true;
                }
            }
        }
    }

    public class C1
    {
        public bool Selected { get; set; }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var fieldDeclaration = syntaxTree.FindFieldDeclaration("c1");
            var field = semanticModel.GetDeclaredSymbolSafe(fieldDeclaration, CancellationToken.None);
            using (var assignedValues = AssignedValueWalker.Borrow(field, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual("null, value", actual);
            }
        }
    }
}
