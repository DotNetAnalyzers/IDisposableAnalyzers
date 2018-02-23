namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal partial class DisposableTests
    {
        internal class IsAssignedWithCreated
        {
            [Test]
            public void LocalSeparateDeclarationAndAssignment()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        internal Foo()
        {
            IDisposable disposable;
            disposable = new Disposable();
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindBestMatch<AssignmentExpressionSyntax>("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalSeparateDeclarationAndAssignmentInLambda()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                IDisposable disposable;
                disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindBestMatch<AssignmentExpressionSyntax>("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }
        }
    }
}
