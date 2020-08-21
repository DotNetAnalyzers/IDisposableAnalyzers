namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class AssignedValueWalkerTests
    {
        public static class Constructors
        {
            [TestCase("var temp1 = this.value;", "")]
            [TestCase("var temp2 = this.value;", "arg")]
            [TestCase("var temp3 = this.value;", "arg")]
            public static void FieldCtorArgSimple(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private readonly int value;

        internal C(int arg)
        {
            var temp1 = this.value;
            this.value = arg;
            var temp2 = this.value;
        }

        internal void M()
        {
            var temp3 = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "")]
            [TestCase("var temp2 = this.value;", "Id(arg)")]
            [TestCase("var temp3 = this.value;", "Id(arg)")]
            public static void FieldCtorArgThenIdMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private readonly int value;

        internal C(int arg)
        {
            var temp1 = this.value;
            this.value = Id(arg);
            var temp2 = this.value;
        }

        internal void M()
        {
            var temp3 = this.value;
        }

        private static T Id<T>(T genericArg) => genericArg;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public static void FieldChainedCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        internal C()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }

        internal C(string text)
            : this()
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = 4;
            var temp5 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1, 2")]
            [TestCase("var temp2 = this.value;", "1, 2, 3")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp4 = this.value;", "1")]
            [TestCase("var temp5 = this.value;", "1, 2")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public static void FieldChainedPrivateCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        internal C()
            : this(2)
        {
            var temp1 = this.value;
            this.value = 3;
            var temp2 = this.value;
            this.value = 4;
            var temp3 = this.value;
        }

        private C(int ctorArg)
        {
            var temp4 = this.value;
            this.value = ctorArg;
            var temp5 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1, 2")]
            [TestCase("var temp2 = this.value;", "1, 2, 3")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp4 = this.value;", "1")]
            [TestCase("var temp5 = this.value;", "1, ctorArg, 2")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, ctorArg, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, ctorArg, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, ctorArg, 5, arg")]
            public static void FieldChainedInternalCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        internal C()
            : this(2)
        {
            var temp1 = this.value;
            this.value = 3;
            var temp2 = this.value;
            this.value = 4;
            var temp3 = this.value;
        }

        internal C(int ctorArg)
        {
            var temp4 = this.value;
            this.value = ctorArg;
            var temp5 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            public static void FieldPrivateCtorFactory(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        private C(int ctorArg)
        {
            var temp1 = this.value;
            this.value = ctorArg;
            var temp2 = this.value;
        }

        internal static C Create()
        {
            return new C(2);
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, ctorArg, 2")]
            public static void FieldPublicCtorFactory(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        public C(int ctorArg)
        {
            var temp1 = this.value;
            this.value = ctorArg;
            var temp2 = this.value;
        }

        internal static C Create()
        {
            return new C(2);
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public static void FieldChainedCtorGenericClass(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C<T>
    {
        public int value = 1;

        internal C()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }

        internal C(string text)
            : this()
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = 4;
            var temp5 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
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
            public static void FieldCtorCallingPrivateInitializeMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        internal C()
        {
            var temp1 = this.value;
            this.Initialize(2);
            var temp4 = this.value;
            this.value = 3;
            var temp5 = this.value;
            this.Initialize(4);
            var temp6 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
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
            public static void FieldCtorCallingProtectedInitializeMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        public int value = 1;

        internal C()
        {
            var temp1 = this.value;
            this.Initialize(2);
            var temp4 = this.value;
            this.value = 3;
            var temp5 = this.value;
            this.Initialize(4);
            var temp6 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
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
            public static void AutoPropertyChainedCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            var temp1 = this.Value;
            this.Value = 2;
            var temp2 = this.Value;
            this.M(3);
            var temp3 = this.Value;
        }

        internal C(string text)
            : this()
        {
            var temp4 = this.Value;
            this.Value = 4;
            var temp5 = this.Value;
            this.Value = 5;
            var temp6 = this.Value;
            this.M(6);
            var temp7 = this.Value;
            this.M(7);
            var temp8 = this.Value;
        }

        public int Value { get; set; } = 1;

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.temp1;", "this.value")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.temp1;", "this.value")]
            [TestCase("var temp5 = this.value;", "1, 2")]
            [TestCase("var temp6 = this.temp1;", "this.value")]
            public static void FieldInitializedlWithLiteralAndAssignedInCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private readonly int value = 1;
        private readonly int temp1;

        internal C()
        {
            this.temp1 = this.value;
            var temp1 = this.value;
            var temp2 = this.temp1;
            this.value = 2;
            var temp3 = this.value;
            var temp4 = this.temp1;
        }

        internal void M()
        {
            var temp5 = this.value;
            var temp6 = this.temp1;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.Value;", "1, 2, 3")]
            [TestCase("var temp2 = this.Value;", "1, 2, 3, 4")]
            public static void InitializedInChainedWithLiteralGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C<T>
    {
        internal C()
        {
            this.Value = 2;
        }

        internal C(string text)
            : this()
        {
            this.Value = 3;
            var temp1 = this.Value;
            this.Value = 4;
        }

        public int Value { get; set; } = 1;

        internal void M()
        {
            var temp2 = this.Value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, arg")]
            [TestCase("var temp4 = this.value;", "1, 2, 3, arg")]
            public static void FieldImplicitBase(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base
    {
        protected int value = 1;

        internal Base()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }
    }

    internal class C : Base
    {
        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            public static void FieldImplicitBaseWhenSubclassHasCtor(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base
    {
        protected int value = 1;

        internal Base()
        {
            var temp1 = this.value;
            this.value = 2;
            var temp2 = this.value;
        }
    }

    internal class C : Base
    {
        internal C()
        {
            var temp3 = this.value;
            this.value = 3;
            var temp4 = this.value;
            this.value = 4;
            var temp5 = this.value;
        }

        internal void M(int arg)
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public static void InitializedInBaseCtorWithLiteral(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base
    {
        protected int value = 1;

        internal Base()
        {
            this.value = 2;
        }

        internal Base(int value)
        {
            this.value = value;
        }
    }

    internal class C : Base
    {
        internal C()
        {
            this.value = 3;
            var temp1 = this.value;
            this.value = 4;
        }

        internal void M()
        {
            var temp2 = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public static void InitializedInExplicitBaseCtorWithLiteral(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base
    {
        protected int value = 1;

        public Base()
        {
            this.value = -1;
        }

        public Base(int value)
        {
            this.value = value;
        }
    }

    internal class C : Base
    {
        internal C()
            : base(2)
        {
            this.value = 3;
            var temp1 = this.value;
            this.value = 4;
        }

        internal void M()
        {
            var temp2 = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public static void InitializedInBaseCtorWithDefaultGenericSimple()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base<T>
    {
        protected readonly T value;

        internal Base()
        {
            this.value = default(T);
        }
    }

    internal class C : Base<int>
    {
        internal C()
        {
            var temp = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = this.value;").Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual("default(T)", actual);
            }

            [Test]
            public static void AbstractGenericInitializedInBaseCtorSimple()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public abstract class Base<T>
    {
        protected Base()
        {
            this.Value = default(T);
        }

        public abstract T Value { get; set; }
    }

    public class C : Base<int>
    {
        public C()
        {
            var temp = this.Value;
        }

        public override int Value { get; set; }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = this.Value;").Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual("default(T)", actual);
            }

            [TestCase("var temp1 = this.value;", "default(T)")]
            [TestCase("var temp2 = this.value;", "default(T)")]
            public static void InitializedInBaseCtorWithDefaultGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base<T>
    {
        protected readonly T value;

        internal Base()
        {
            this.value = default(T);
        }

        internal Base(T value)
        {
            this.value = value;
        }
    }

    internal class C : Base<int>
    {
        internal C()
        {
            var temp1 = this.value;
        }

        internal void M()
        {
            var temp2 = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("var temp1 = this.value;", "default(T)")]
            [TestCase("var temp2 = this.value;", "default(T)")]
            public static void InitializedInBaseCtorWithDefaultGenericGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class Base<T>
    {
        protected readonly T value;

        internal Base()
        {
            this.value = default(T);
        }

        internal Base(T value)
        {
            this.value = value;
        }
    }

    internal class C<T> : Base<T>
    {
        internal C()
        {
            var temp1 = this.value;
        }

        internal void M()
        {
            var temp2 = this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                var actual = string.Join(", ", assignedValues);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public static void FieldAssignedInLambdaCtor()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;

    public class C
    {
        private int value;

        public C()
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
                using var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None);
                Assert.AreEqual("1", assignedValues.Single().ToString());
            }
        }
    }
}
