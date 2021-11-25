namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class AssignedValueWalkerTests
    {
        [TestCase("var temp1 = this.P;", "1")]
        [TestCase("var temp2 = this.P;", "1, 2")]
        [TestCase("var temp3 = this.P;", "1, 2")]
        public static void AutoPropertyGetSetAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        public C()
        {
            var temp1 = this.P;
            this.P = 2;
            var temp2 = this.P;
        }

        public int P { get; set; } = 1;

        public void M()
        {
            var temp3 = this.P;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("var temp1 = this.P;", "1")]
        [TestCase("var temp2 = this.P;", "1, 2")]
        [TestCase("var temp3 = this.P;", "1, 2")]
        public static void AutoPropertyGetOnlyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        public C()
        {
            var temp1 = this.P;
            this.P = 2;
            var temp2 = this.P;
        }

        public int P { get; } = 1;

        public void M()
        {
            var temp3 = this.P;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("var temp1 = this.p;", "1")]
        [TestCase("var temp2 = this.P;", "")]
        [TestCase("var temp3 = this.p;", "1, 2")]
        [TestCase("var temp4 = this.P;", "")]
        [TestCase("var temp5 = this.p;", "1, 2")]
        [TestCase("var temp6 = this.P;", "")]
        public static void BackingFieldPrivateSetInitializedAndAssignedInCtor(string code1, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        private int p = 1;

        public C()
        {
            var temp1 = this.p;
            var temp2 = this.P;
            this.p = 2;
            var temp3 = this.p;
            var temp4 = this.P;
        }

        public int P
        {
            get { return this.p; }
            private set { this.p = value; }
        }

        public void M()
        {
            var temp5 = this.p;
            var temp6 = this.P;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code1).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("var temp1 = this.p;", "1")]
        [TestCase("var temp2 = this.P;", "")]
        [TestCase("var temp3 = this.p;", "1, 2")]
        [TestCase("var temp4 = this.P;", "")]
        [TestCase("var temp5 = this.p;", "1, 2, value")]
        [TestCase("var temp6 = this.P;", "")]
        public static void BackingFieldPublicSetInitializedAndAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        private int p = 1;

        public C()
        {
            var temp1 = this.p;
            var temp2 = this.P;
            this.p = 2;
            var temp3 = this.p;
            var temp4 = this.P;
        }

        public int P
        {
            get { return this.p; }
            set { this.p = value; }
        }

        public void M()
        {
            var temp5 = this.p;
            var temp6 = this.P;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void BackingFieldPublicSetSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        private int p;

        public int P
        {
            get { return this.p; }
            set { this.p = value; }
        }

        public void M()
        {
            var temp = this.p;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = this.p").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual("value", actual);
        }

        [Test]
        public static void BackingFieldPrivateSetSimple()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        private int p;

        public int P
        {
            get { return this.p; }
            private set { this.p = value; }
        }

        public void M()
        {
            var temp = this.p;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var temp = this.p").Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(string.Empty, actual);
        }

        [TestCase("var temp1 = this.p;", "1")]
        [TestCase("var temp2 = this.P;", "")]
        [TestCase("var temp3 = this.p;", "1, 2")]
        [TestCase("var temp4 = this.P;", "2")]
        [TestCase("var temp5 = this.p;", "1, 2")]
        [TestCase("var temp6 = this.P;", "2")]
        public static void BackingFieldPrivateSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class C
{
    private int p = 1;

    public C()
    {
        var temp1 = this.p;
        var temp2 = this.P;
        this.P = 2;
        var temp3 = this.p;
        var temp4 = this.P;
    }

    public int P
    {
        get { return this.p; }
        private set { this.p = value; }
    }

    public void M()
    {
        var temp5 = this.p;
        var temp6 = this.P;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("var temp1 = this.p;", "1")]
        [TestCase("var temp2 = this.P;", "")]
        [TestCase("var temp3 = this.p;", "1, 2")]
        [TestCase("var temp4 = this.P;", "2")]
        [TestCase("var temp5 = this.p;", "1, 2, value")]
        [TestCase("var temp6 = this.P;", "2")]
        public static void BackingFieldPublicSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class C
{
    private int p = 1;

    public C()
    {
        var temp1 = this.p;
        var temp2 = this.P;
        this.P = 2;
        var temp3 = this.p;
        var temp4 = this.P;
    }

    public int P
    {
        get { return this.p; }
        set { this.p = value; }
    }

    public void M()
    {
        var temp5 = this.p;
        var temp6 = this.P;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("var temp1 = this.p;", "1")]
        [TestCase("var temp2 = this.P;", "")]
        [TestCase("var temp3 = this.p;", "1, 2, value / 2, 3")]
        [TestCase("var temp4 = this.P;", "2")]
        [TestCase("var temp5 = this.p;", "1, 2, value / 2, 3, value, value")]
        [TestCase("var temp6 = this.P;", "2")]
        public static void BackingFieldPublicSetInitializedAndPropertyAssignedInCtorWeirdSetter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        private int p = 1;

        public C()
        {
            var temp1 = this.p;
            var temp2 = this.P;
            this.P = 2;
            var temp3 = this.p;
            var temp4 = this.P;
        }

        public int P
        {
            get { return this.p; }
            set
            {
                if (true)
                {
                    this.p = value;
                }
                else
                {
                    this.p = value;
                }

                this.p = value / 2;
                this.p = 3;
            }
        }

        public void M()
        {
            var temp5 = this.p;
            var temp6 = this.P;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("var temp1 = this.P;", "")]
        [TestCase("var temp2 = this.P;", "2")]
        [TestCase("var temp3 = this.P;", "2, value")]
        public static void RecursiveGetAndSet(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public sealed class C
    {
        public C()
        {
            var temp1 = this.P;
            this.P = 2;
            var temp2 = this.P;
        }

        public int P
        {
            get { return this.P; }
            set { this.P = value; }
        }

        public void M()
        {
            var temp3 = this.P;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using var walker = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Recursive()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
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
            using var walker = AssignedValueWalker.Borrow(field, semanticModel, CancellationToken.None);
            var actual = string.Join(", ", walker.Values);
            Assert.AreEqual("null, value", actual);
        }
    }
}
