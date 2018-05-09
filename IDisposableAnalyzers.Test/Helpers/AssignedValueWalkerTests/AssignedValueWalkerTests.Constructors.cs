namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Constructors
        {
            [TestCase("var temp1 = this.value;", "")]
            [TestCase("var temp2 = this.value;", "arg")]
            [TestCase("var temp3 = this.value;", "arg")]
            public void FieldCtorArgSimple(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        internal Foo(int arg)
        {
            var temp1 = this.value;
            this.value = arg;
            var temp2 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp3 = this.value;
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

            [TestCase("var temp1 = this.value;", "")]
            [TestCase("var temp2 = this.value;", "Id(arg)")]
            [TestCase("var temp3 = this.value;", "Id(arg)")]
            public void FieldCtorArgThenIdMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        internal Foo(int arg)
        {
            var temp1 = this.value;
            this.value = Id(arg);
            var temp2 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp3 = this.value;
        }

        private static T Id<T>(T genericArg) => genericArg;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldChainedCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        internal Foo()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }

        internal Foo(string text)
            : this()
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = 4;
            var temp5 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp6 = this.value;
            this.value = 5;
            var temp7 = this.value;
            this.value = arg;
            var temp8 = this.value;
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

            [TestCase("var temp1 = this.value;", "1, 2")]
            [TestCase("var temp2 = this.value;", "1, 2, 3")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp4 = this.value;", "1")]
            [TestCase("var temp5 = this.value;", "1, 2")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldChainedPrivateCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        internal Foo()
            : this(2)
        {
            var temp1 = this.value;
            this.value = 3;
            var temp2 = this.value;
            this.value = 4;
            var temp3 = this.value;
        }

        private Foo(int ctorArg)
        {
            var temp4 = this.value;
            this.value = ctorArg;
            var temp5 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp6 = this.value;
            this.value = 5;
            var temp7 = this.value;
            this.value = arg;
            var temp8 = this.value;
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

            [TestCase("var temp1 = this.value;", "1, 2")]
            [TestCase("var temp2 = this.value;", "1, 2, 3")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp4 = this.value;", "1")]
            [TestCase("var temp5 = this.value;", "1, ctorArg, 2")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, ctorArg, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, ctorArg, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, ctorArg, 5, arg")]
            public void FieldChainedInternalCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        internal Foo()
            : this(2)
        {
            var temp1 = this.value;
            this.value = 3;
            var temp2 = this.value;
            this.value = 4;
            var temp3 = this.value;
        }

        internal Foo(int ctorArg)
        {
            var temp4 = this.value;
            this.value = ctorArg;
            var temp5 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp6 = this.value;
            this.value = 5;
            var temp7 = this.value;
            this.value = arg;
            var temp8 = this.value;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            public void FieldPrivateCtorFactory(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        private Foo(int ctorArg)
        {
            var temp1 = this.value;
            this.value = ctorArg;
            var temp2 = this.value;
        }

        internal static Foo Create()
        {
            return new Foo(2);
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, ctorArg, 2")]
            public void FieldPublicCtorFactory(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        public Foo(int ctorArg)
        {
            var temp1 = this.value;
            this.value = ctorArg;
            var temp2 = this.value;
        }

        internal static Foo Create()
        {
            return new Foo(2);
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldChainedCtorGenericClass(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo<T>
    {
        public int value = 1;

        internal Foo()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }

        internal Foo(string text)
            : this()
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = 4;
            var temp5 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp6 = this.value;
            this.value = 5;
            var temp7 = this.value;
            this.value = arg;
            var temp8 = this.value;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp4 = this.value;", "1, 2")]
            [TestCase("var temp5 = this.value;", "1, 2, 3")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp9 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldCtorCallingPrivateInitializeMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        internal Foo()
        {
            var temp1 = this.value;
            this.Initialize(2);
            var temp4 = this.value;
            this.value = 3;
            var temp5 = this.value;
            this.Initialize(4);
            var temp6 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp7 = this.value;
            this.value = 5;
            var temp8 = this.value;
            this.value = arg;
            var temp9 = this.value;
        }

        private void Initialize(int initArg)
        {
            var temp2 = this.value;
            this.value = initArg;
            var temp3 = this.value;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4, 5, arg, initArg")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, 4, 5, arg, initArg")]
            [TestCase("var temp4 = this.value;", "1, 2")]
            [TestCase("var temp5 = this.value;", "1, 2, 3")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg, initArg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg, initArg")]
            [TestCase("var temp9 = this.value;", "1, 2, 3, 4, 5, arg, initArg")]
            public void FieldCtorCallingProtectedInitializeMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        public int value = 1;

        internal Foo()
        {
            var temp1 = this.value;
            this.Initialize(2);
            var temp4 = this.value;
            this.value = 3;
            var temp5 = this.value;
            this.Initialize(4);
            var temp6 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp7 = this.value;
            this.value = 5;
            var temp8 = this.value;
            this.value = arg;
            var temp9 = this.value;
        }

        protected void Initialize(int initArg)
        {
            var temp2 = this.value;
            this.value = initArg;
            var temp3 = this.value;
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

            [TestCase("var temp1 = this.Value;", "1")]
            [TestCase("var temp2 = this.Value;", "1, 2")]
            [TestCase("var temp3 = this.Value;", "1, 2, 8, 3")]
            [TestCase("var temp4 = this.Value;", "1, 2, 8, 3")]
            [TestCase("var temp5 = this.Value;", "1, 2, 8, 3, 4")]
            [TestCase("var temp6 = this.Value;", "1, 2, 8, 3, 4, 5")]
            [TestCase("var temp7 = this.Value;", "1, 2, 8, 3, 4, 5, 6")]
            [TestCase("var temp8 = this.Value;", "1, 2, 8, 3, 4, 5, 6, 7")]
            [TestCase("var temp9 = this.Value;", "1, 2, 8, 3, 4, 5, 6, 7, arg")]
            [TestCase("var temp10 = this.Value;", "1, 2, 8, 3, 4, 5, 6, 7, arg")]
            [TestCase("var temp11 = this.Value;", "1, 2, 8, 3, 4, 5, 6, 7, arg")]
            public void AutoPropertyChainedCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp1 = this.Value;
            this.Value = 2;
            var temp2 = this.Value;
            this.Bar(3);
            var temp3 = this.Value;
        }

        internal Foo(string text)
            : this()
        {
            var temp4 = this.Value;
            this.Value = 4;
            var temp5 = this.Value;
            this.Value = 5;
            var temp6 = this.Value;
            this.Bar(6);
            var temp7 = this.Value;
            this.Bar(7);
            var temp8 = this.Value;
        }

        public int Value { get; set; } = 1;

        internal void Bar(int arg)
        {
            var temp9 = this.Value;
            this.Value = 8;
            var temp10 = this.Value;
            this.Value = arg;
            var temp11 = this.Value;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.temp1;", "this.value")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.temp1;", "this.value")]
            [TestCase("var temp5 = this.value;", "1, 2")]
            [TestCase("var temp6 = this.temp1;", "this.value")]
            public void FieldInitializedlWithLiteralAndAssignedInCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value = 1;
        private readonly int temp1;

        internal Foo()
        {
            this.temp1 = this.value;
            var temp1 = this.value;
            var temp2 = this.temp1;
            this.value = 2;
            var temp3 = this.value;
            var temp4 = this.temp1;
        }

        internal void Bar()
        {
            var temp5 = this.value;
            var temp6 = this.temp1;
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

            [TestCase("var temp1 = this.Value;", "1, 2, 3")]
            [TestCase("var temp2 = this.Value;", "1, 2, 3, 4")]
            public void InitializedInChainedWithLiteralGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo<T>
    {
        internal Foo()
        {
            this.Value = 2;
        }

        internal Foo(string text)
            : this()
        {
            this.Value = 3;
            var temp1 = this.Value;
            this.Value = 4;
        }

        public int Value { get; set; } = 1;

        internal void Bar()
        {
            var temp2 = this.Value;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, arg")]
            [TestCase("var temp4 = this.value;", "1, 2, 3, arg")]
            public void FieldImplicitBase(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase
    {
        protected int value = 1;

        internal FooBase()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }
    }

    internal class Foo : FooBase
    {
        internal void Bar(int arg)
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = arg;
            var temp5 = this.value;
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

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldImplicitBaseWhenSubclassHasCtor(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase
    {
        protected int value = 1;

        internal FooBase()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }
    }

    internal class Foo : FooBase
    {
        internal Foo()
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = 4;
            var temp5 = this.value;
        }

        internal void Bar(int arg)
        {
            var temp6 = this.value;
            this.value = 5;
            var temp7 = this.value;
            this.value = arg;
            var temp8 = this.value;
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

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public void InitializedInBaseCtorWithLiteral(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase
    {
        protected int value = 1;

        internal FooBase()
        {
            this.value = 2;
        }

        internal FooBase(int value)
        {
            this.value = value;
        }
    }

    internal class Foo : FooBase
    {
        internal Foo()
        {
            this.value = 3;
            var temp1 = this.value;
            this.value = 4;
        }

        internal void Bar()
        {
            var temp2 = this.value;
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

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public void InitializedInExplicitBaseCtorWithLiteral(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase
    {
        protected int value = 1;

        public FooBase()
        {
            this.value = -1;
        }

        public FooBase(int value)
        {
            this.value = value;
        }
    }

    internal class Foo : FooBase
    {
        internal Foo()
            : base(2)
        {
            this.value = 3;
            var temp1 = this.value;
            this.value = 4;
        }

        internal void Bar()
        {
            var temp2 = this.value;
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
            public void InitializedInBaseCtorWithDefaultGenericSimple()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase<T>
    {
        protected readonly T value;

        internal FooBase()
        {
            this.value = default(T);
        }
    }

    internal class Foo : FooBase<int>
    {
        internal Foo()
        {
            var temp = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = this.value;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("default(T)", actual);
                }
            }

            [TestCase("var temp1 = this.value;", "default(T)")]
            [TestCase("var temp2 = this.value;", "default(T)")]
            public void InitializedInBaseCtorWithDefaultGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase<T>
    {
        protected readonly T value;

        internal FooBase()
        {
            this.value = default(T);
        }

        internal FooBase(T value)
        {
            this.value = value;
        }
    }

    internal class Foo : FooBase<int>
    {
        internal Foo()
        {
            var temp1 = this.value;
        }

        internal void Bar()
        {
            var temp2 = this.value;
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

            [TestCase("var temp1 = this.value;", "default(T)")]
            [TestCase("var temp2 = this.value;", "default(T)")]
            public void InitializedInBaseCtorWithDefaultGenericGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class FooBase<T>
    {
        protected readonly T value;

        internal FooBase()
        {
            this.value = default(T);
        }

        internal FooBase(T value)
        {
            this.value = value;
        }
    }

    internal class Foo<T> : FooBase<T>
    {
        internal Foo()
        {
            var temp1 = this.value;
        }

        internal void Bar()
        {
            var temp2 = this.value;
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
            public void FieldAssignedInLambdaCtor()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private int value;

        public Foo()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                this.value = 1;
            };
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.value = 1").Left;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    Assert.AreEqual("1", assignedValues.Single().ToString());
                }
            }
        }
    }
}
