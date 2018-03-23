namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class MemberPathTests
    {
        internal class TryFindRootMember
        {
            [TestCase("this.foo", "this.foo")]
            [TestCase("foo", "foo")]
            [TestCase("foo.Inner", "foo")]
            [TestCase("this.foo.Inner", "this.foo")]
            [TestCase("foo.Inner.foo", "foo")]
            [TestCase("foo.Inner.foo.Inner", "foo")]
            [TestCase("this.foo.Inner.foo.Inner", "this.foo")]
            [TestCase("this.foo?.Inner.foo.Inner", "this.foo")]
            [TestCase("this.foo?.Inner?.foo.Inner", "this.foo")]
            [TestCase("this.foo?.Inner?.foo?.Inner", "this.foo")]
            [TestCase("this.foo.Inner?.foo.Inner", "this.foo")]
            [TestCase("this.foo.Inner?.foo?.Inner", "this.foo")]
            [TestCase("this.foo.Inner.foo?.Inner", "this.foo")]
            [TestCase("(meh as Foo)?.Inner", "meh")]
            public void ForPropertyOrField(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly object meh;
        private readonly Foo foo;

        public Foo Inner => this.foo;

        public void Bar()
        {
            var temp = foo.Inner;
        }
    }
}";
                testCode = testCode.AssertReplace("foo.Inner", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var value = syntaxTree.FindEqualsValueClause("var temp = ").Value;
                Assert.AreEqual(true, MemberPath.TryFindRootMember(value, out ExpressionSyntax member));
                Assert.AreEqual(expected, member.ToString());

                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var symbol = semanticModel.GetSymbolSafe(member, CancellationToken.None);
                Assert.AreEqual(expected.Split('.').Last(), symbol.Name);
            }

            [TestCase("foo.Get<int>(1)", "foo")]
            [TestCase("this.foo.Get<int>(1)", "this.foo")]
            [TestCase("this.foo.Inner.Get<int>(1)", "this.foo")]
            [TestCase("this.foo.Inner.foo.Get<int>(1)", "this.foo")]
            [TestCase("this.foo?.Get<int>(1)", "this.foo")]
            [TestCase("this.foo?.foo.Get<int>(1)", "this.foo")]
            [TestCase("this.Inner?.Inner.Get<int>(1)", "this.Inner")]
            [TestCase("this.Inner?.foo.Get<int>(1)", "this.Inner")]
            [TestCase("this.Inner?.foo?.Get<int>(1)", "this.Inner")]
            [TestCase("this.Inner.foo?.Get<int>(1)", "this.Inner")]
            [TestCase("this.Inner?.foo?.Inner?.Get<int>(1)", "this.Inner")]
            [TestCase("((Foo)meh).Get<int>(1)", "meh")]
            [TestCase("((Foo)this.meh).Get<int>(1)", "this.meh")]
            [TestCase("((Foo)this.Inner.meh).Get<int>(1)", "this.Inner")]
            [TestCase("(meh as Foo).Get<int>(1)", "meh")]
            [TestCase("(this.meh as Foo).Get<int>(1)", "this.meh")]
            [TestCase("(this.Inner.meh as Foo).Get<int>(1)", "this.Inner")]
            [TestCase("(this.Inner.meh as Foo)?.Get<int>(1)", "this.Inner")]
            [TestCase("(meh as Foo)?.Get<int>(1)", "meh")]
            public void ForMethodInvocation(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly object meh;
        private readonly Foo foo;

        public Foo Inner => this.foo;

        public void Dispose()
        {
            var temp = this.foo.Get<int>(1);
        }

        private T Get<T>(int value) => default(T);
    }
}";
                testCode = testCode.AssertReplace("this.foo.Get<int>(1)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var invocation = syntaxTree.FindInvocation("Get<int>(1)");
                Assert.AreEqual(true, MemberPath.TryFindRootMember(invocation, out ExpressionSyntax member));
                Assert.AreEqual(expected, member.ToString());

                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var symbol = semanticModel.GetSymbolSafe(member, CancellationToken.None);
                Assert.AreEqual(expected.Split('.').Last(), symbol.Name);
            }

            [Test]
            public void Recursive()
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int value;

        public int Value
        {
            get { return this.Value; }
            set { this.Value = value; }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var invocation = syntaxTree.FindMemberAccessExpression("this.Value");
                Assert.AreEqual(true, MemberPath.TryFindRootMember(invocation, out ExpressionSyntax member));
                Assert.AreEqual("this.Value", member.ToString());
            }
        }
    }
}
