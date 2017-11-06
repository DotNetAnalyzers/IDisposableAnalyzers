namespace IDisposableAnalyzers.Test.Helpers.SyntaxtTreeHelpersTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public class InvocationExpressionSyntaxExtTests
    {
        [TestCase("Method1()", "Method1")]
        [TestCase("this.Method1()", "Method1")]
        [TestCase("new Foo()?.Method1()", "Method1")]
        [TestCase("this.Method2<int>()", "Method2")]
        public void TryGetInvokedMethodName(string code, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var i = Method1();
            i = this.Method1();
            i = new Foo()?.Method1() ?? 0;
            i = Method2<int>();
            i = this.Method2<int>();
        }

        private int Method1() => 1;

        private int Method2<T>() => 2;
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var invocation = syntaxTree.FindInvocation(code);
            Assert.AreEqual(true, invocation.TryGetInvokedMethodName(out var name));
            Assert.AreEqual(expected, name);
        }
    }
}