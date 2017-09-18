namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal class ConstructorTests
    {
        internal class IsRunBefore
        {
            [TestCase("Foo()", "Foo(int intValue)", true)]
            [TestCase("Foo()", "Foo(string textValue)", true)]
            [TestCase("Foo(int intValue)", "Foo(string textValue)", true)]
            [TestCase("Foo(int intValue)", "Foo()", false)]
            [TestCase("Foo(string textValue)", "Foo()", false)]
            [TestCase("Foo()", "Foo()", false)]
            [TestCase("Foo(int intValue)", "Foo(int intValue)", false)]
            [TestCase("Foo(string textValue)", "Foo(string textValue)", false)]
            public void ChainedInSameType(string code1, string code2, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
        }

        internal Foo(int intValue)
            : this()
        {
        }

        internal Foo(string textValue)
            : this(1)
        {
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.BestMatch<ConstructorDeclarationSyntax>(code1);
                var other = syntaxTree.BestMatch<ConstructorDeclarationSyntax>(code2);
                Assert.AreEqual(expected, Constructor.IsRunBefore(first, other, semanticModel, CancellationToken.None));
            }

            [TestCase("FooBase()", "Foo()", true)]
            [TestCase("FooBase()", "Foo(int intValue)", true)]
            [TestCase("FooBase()", "Foo(string textValue)", true)]
            [TestCase("FooBase(int intValue)", "Foo()", false)]
            [TestCase("FooBase(int intValue)", "Foo(int intValue)", false)]
            [TestCase("FooBase(int intValue)", "Foo(string textValue)", false)]
            [TestCase("FooBase(string textValue)", "Foo()", false)]
            [TestCase("FooBase(string textValue)", "Foo(int intValue)", false)]
            [TestCase("FooBase(string textValue)", "Foo(string textValue)", false)]
            [TestCase("Foo()", "FooBase(int intValue)", false)]
            [TestCase("Foo(int intValue)", "FooBase(int intValue)", false)]
            [TestCase("Foo(string textValue)", "FooBase(int intValue)", false)]
            [TestCase("Foo()", "FooBase(string textValue)", false)]
            [TestCase("Foo(int intValue)", "FooBase(string textValue)", false)]
            [TestCase("Foo(string textValue)", "FooBase(string textValue)", false)]
            public void ImplicitDefaultBase(string code1, string code2, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase
    {
        internal FooBase()
        {
        }

        internal FooBase(int intValue)
            : this()
        {
        }

        internal FooBase(string textValue)
            : this(1)
        {
        }
    }

    internal class Foo : FooBase
    {
        internal Foo()
        {
        }

        internal Foo(int intValue)
            : this()
        {
        }

        internal Foo(string textValue)
            : this(1)
        {
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.BestMatch<ConstructorDeclarationSyntax>(code1);
                var other = syntaxTree.BestMatch<ConstructorDeclarationSyntax>(code2);
                Assert.AreEqual(expected, Constructor.IsRunBefore(first, other, semanticModel, CancellationToken.None));
            }

            [TestCase("FooBase()", "Foo()", true)]
            [TestCase("FooBase()", "Foo(int intValue)", true)]
            [TestCase("FooBase()", "Foo(string textValue)", true)]
            [TestCase("FooBase(int intValue)", "Foo()", false)]
            [TestCase("FooBase(int intValue)", "Foo(int intValue)", false)]
            [TestCase("FooBase(int intValue)", "Foo(string textValue)", false)]
            [TestCase("FooBase(string textValue)", "Foo()", false)]
            [TestCase("FooBase(string textValue)", "Foo(int intValue)", false)]
            [TestCase("FooBase(string textValue)", "Foo(string textValue)", false)]
            [TestCase("Foo()", "FooBase(int intValue)", false)]
            [TestCase("Foo(int intValue)", "FooBase(int intValue)", false)]
            [TestCase("Foo(string textValue)", "FooBase(int intValue)", false)]
            [TestCase("Foo()", "FooBase(string textValue)", false)]
            [TestCase("Foo(int intValue)", "FooBase(string textValue)", false)]
            [TestCase("Foo(string textValue)", "FooBase(string textValue)", false)]
            public void ExplicitDefaultBase(string code1, string code2, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase
    {
        internal FooBase()
        {
        }

        internal FooBase(int intValue)
            : this()
        {
        }

        internal FooBase(string textValue)
            : this(1)
        {
        }
    }

    internal class Foo : FooBase
    {
        internal Foo()
            : base()
        {
        }

        internal Foo(int intValue)
            : this()
        {
        }

        internal Foo(string textValue)
            : this(1)
        {
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.BestMatch<ConstructorDeclarationSyntax>(code1);
                var other = syntaxTree.BestMatch<ConstructorDeclarationSyntax>(code2);
                Assert.AreEqual(expected, Constructor.IsRunBefore(first, other, semanticModel, CancellationToken.None));
            }

            [TestCase("Foo()", "Foo()", false)]
            [TestCase("Foo()", "Foo(int value)", true)]
            [TestCase("Foo(int value)", "Foo()", false)]
            [TestCase("Foo()", "Foo(string text)", true)]
            [TestCase("Foo(string text)", "Foo()", false)]
            [TestCase("Foo(int value)", "Foo(string text)", true)]
            [TestCase("Foo(string text)", "Foo(int value)", false)]
            public void ThisChained(string firstSignature, string otherSignature, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : this(1)
    {
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
                var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
                Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
            }

            [TestCase("Foo()", "Foo()", false)]
            [TestCase("Foo()", "Foo(int value)", false)]
            [TestCase("Foo(int value)", "Foo()", false)]
            [TestCase("Foo()", "Foo(string text)", false)]
            [TestCase("Foo(string text)", "Foo()", false)]
            [TestCase("Foo(int value)", "Foo(string text)", false)]
            [TestCase("Foo(int value)", "Foo(int value)", false)]
            [TestCase("Foo(string text)", "Foo(int value)", false)]
            public void WhenNotChained(string firstSignature, string otherSignature, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
    }

    internal Foo(int value)
    {
    }

    internal Foo(string text)
    {
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
                var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
                Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
            }

            [TestCase("Foo()", "FooBase()", false)]
            [TestCase("FooBase()", "Foo()", true)]
            [TestCase("FooBase()", "Foo(int value)", true)]
            [TestCase("FooBase()", "Foo(string text)", true)]
            [TestCase("FooBase(int value)", "Foo()", false)]
            [TestCase("FooBase(int value)", "Foo(int value)", false)]
            [TestCase("FooBase(int value)", "Foo(string text)", false)]
            public void BaseImplicit(string firstSignature, string otherSignature, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
    }

    internal FooBase(int value)
        : this()
    {
    }

    internal FooBase(string text)
        : this(1)
    {
    }
}

internal class Foo : FooBase
{
    internal Foo()
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : this(1)
    {
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
                var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
                Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
            }

            [TestCase("Foo()", "FooBaseBase()", false)]
            [TestCase("FooBaseBase()", "Foo()", true)]
            [TestCase("FooBaseBase()", "Foo(int value)", true)]
            [TestCase("FooBaseBase()", "Foo(string text)", true)]
            [TestCase("FooBaseBase(int value)", "Foo()", false)]
            [TestCase("FooBaseBase(int value)", "Foo(int value)", false)]
            [TestCase("FooBaseBase(int value)", "Foo(string text)", false)]
            public void TwoLevelBaseImplicit(string firstSignature, string otherSignature, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBaseBase
{
    internal FooBaseBase()
    {
    }

    internal FooBaseBase(int value)
        : this()
    {
    }

    internal FooBaseBase(string text)
        : this(1)
    {
    }
}

internal class FooBase : FooBaseBase
{
}

internal class Foo : FooBase
{
    internal Foo()
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : this(1)
    {
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
                var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
                Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
            }

            [TestCase("FooBase()", "Foo()", true)]
            [TestCase("FooBase(int value)", "Foo()", true)]
            [TestCase("FooBase()", "Foo(int value)", true)]
            [TestCase("FooBase(int value)", "Foo(int value)", true)]
            [TestCase("FooBase(string text)", "Foo(string text)", true)]
            [TestCase("FooBase(int value)", "Foo(string text)", true)]
            [TestCase("FooBase()", "Foo(string text)", true)]
            [TestCase("Foo()", "FooBase()", false)]
            [TestCase("FooBase(string text)", "Foo()", false)]
            [TestCase("FooBase(string text)", "Foo(int value)", false)]
            public void BaseExplicit(string firstSignature, string otherSignature, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
    }

    internal FooBase(int value)
        : this()
    {
    }

    internal FooBase(string text)
        : this(1)
    {
    }
}

internal class Foo : FooBase
{
    internal Foo()
        : base(1)
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : base(text)
    {
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
                var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
                Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
            }
        }
    }
}