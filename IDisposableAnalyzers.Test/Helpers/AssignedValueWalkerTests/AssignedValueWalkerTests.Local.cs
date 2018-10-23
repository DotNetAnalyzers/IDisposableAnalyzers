namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Local
        {
            [TestCase("1")]
            [TestCase("1 + 1")]
            [TestCase("Value")]
            [TestCase("abc")]
            [TestCase("default(int)")]
            [TestCase("typeof(int)")]
            [TestCase("nameof(int)")]
            [TestCase("new int[] { 1 , 2 , 3 }")]
            [TestCase("new int[2]")]
            public void InitializedWithConstant(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private const int Value = 2;

        internal Foo()
        {
            var value = 1;
            var temp = value;
        }
    }
}";
                testCode = testCode.AssertReplace("1", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = value;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    Assert.AreEqual(code, assignedValues.Single().ToString());
                }
            }

            [Test]
            public void InitializedWithDefaultGeneric()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo<T>
    {
        internal Foo()
        {
            var value = default(T);
            var temp = value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = value;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("default(T)", actual);
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "1")]
            public void NotInitialized(string code, string expected)
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
            value = 1;
            var temp2 = value;
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

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "1")]
            public void NotInitializedInLambda(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                int value;
                var temp1 = value;
                value = 1;
                var temp2 = value;
            };
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

            [TestCase("var temp1 = value;", "1, 2")]
            [TestCase("var temp2 = value;", "1, 2")]
            public void LambdaClosure(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            int value = 1;
            Console.CancelKeyPress += (o, e) =>
            {
                var temp1 = value;
                value = 2;
                var temp2 = value;
            };
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

            [TestCase("var temp1 = value;", "1, 2")]
            [TestCase("var temp2 = value;", "1, 2")]
            public void Loop(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int i)
        {
            int value = 1;
            while (i > 0)
            {
                var temp1 = value;
                value = 2;
                var temp2 = value;
                i--;
            }
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
            public void AssignedWithArg()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo(int meh)
        {
            var temp = meh;
            var value = temp;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = temp").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("meh", actual);
                }
            }

            [Test]
            public void VerbatimIdentifierAssignedWithArg()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo(int meh)
        {
            var @operator = meh;
            var value = @operator;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = @operator").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    Assert.AreEqual("meh", assignedValues.Single().ToString());
                }
            }

            [Test]
            public void AssignedWithArgGenericMethod()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal void Bar<T>(T meh)
        {
            var temp = meh;
            var value = temp;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = temp").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("meh", actual);
                }
            }

            [Test]
            public void AssignedWithArgGenericClass()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo<T>
    {
        internal Foo(T meh)
        {
            var temp = meh;
            var value = temp;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = temp").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("meh", actual);
                }
            }

            [Test]
            public void AssignedInLock()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly object gate;

        public IDisposable disposable;
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            var toDispose = (IDisposable)null;
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                toDispose = this.disposable;
                this.disposable = null;
            }

            var temp = toDispose;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = toDispose;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("(IDisposable)null, this.disposable", actual);
                }
            }
        }
    }
}
