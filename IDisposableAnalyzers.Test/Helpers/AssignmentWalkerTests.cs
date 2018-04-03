namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public partial class AssignmentWalkerTests
    {
        [Test]
        public void Recursive()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        private Bar bar1;
        private Bar bar2;

        public Bar Bar1
        {
            get
            {
                return this.bar1;
            }

            set
            {
                if (Equals(value, this.bar1))
                {
                    return;
                }

                if (value != null && this.bar2 != null)
                {
                    this.Bar2 = null;
                }

                if (this.bar1 != null)
                {
                    this.bar1.Selected = false;
                }

                this.bar1 = value;
                if (this.bar1 != null)
                {
                    this.bar1.Selected = true;
                }
            }
        }

        public Bar Bar2
        {
            get
            {
                return this.bar2;
            }

            set
            {
                if (Equals(value, this.bar2))
                {
                    return;
                }

                if (value != null && this.bar1 != null)
                {
                    this.Bar1 = null;
                }

                if (this.bar2 != null)
                {
                    this.bar2.Selected = false;
                }

                this.bar2 = value;
                if (this.bar2 != null)
                {
                    this.bar2.Selected = true;
                }
            }
        }
    }

    public class Bar
    {
        public bool Selected { get; set; }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var setter = syntaxTree.FindPropertyDeclaration("Bar2").Find<AccessorDeclarationSyntax>("set");
            using (var assignedValues = AssignmentWalker.Borrow(setter, Search.Recursive, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", assignedValues.Assignments);
                var expected = "this.Bar2 = null, this.Bar1 = null, this.Bar2 = null, this.bar1.Selected = false, this.bar1 = value, this.bar1.Selected = true, this.bar2.Selected = false, this.bar2 = value, this.bar2.Selected = true, this.bar1.Selected = false, this.bar1 = value, this.bar1.Selected = true";
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
