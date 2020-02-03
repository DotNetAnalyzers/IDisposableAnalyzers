namespace IDisposableAnalyzers.Test.Helpers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static class DisposeMethodTests
    {
        [TestCase(Search.TopLevel)]
        [TestCase(Search.Recursive)]
        public static void Find(Search search)
        {
            var code = @"
namespace N
{
    using System;

    internal sealed class C : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var method = semanticModel.GetDeclaredSymbol(syntaxTree.FindClassDeclaration("C"));
            Assert.AreEqual("N.C.Dispose()", DisposeMethod.Find(method, compilation, search).ToString());
        }

        [Ignore("Not sure if we want to find explicit.")]
        [TestCase(Search.TopLevel)]
        [TestCase(Search.Recursive)]
        public static void FindWhenExplicit(Search search)
        {
            var code = @"
namespace N
{
    using System;

    internal sealed class C : IDisposable
    {
        private bool disposed;

        void IDisposable.Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var method = semanticModel.GetDeclaredSymbol(syntaxTree.FindClassDeclaration("C"));
            Assert.AreEqual("N.C.Dispose()", DisposeMethod.Find(method, compilation, search).ToString());
        }

        [TestCase(Search.TopLevel)]
        [TestCase(Search.Recursive)]
        public static void FindVirtual(Search search)
        {
            var code = @"
namespace N
{
    using System;

    internal class C : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var method = semanticModel.GetDeclaredSymbol(syntaxTree.FindClassDeclaration("C"));
            Assert.AreEqual("N.C.Dispose(bool)", DisposeMethod.FindVirtual(method, compilation, search).ToString());
        }

        [TestCase(Search.TopLevel)]
        [TestCase(Search.Recursive)]
        public static void FindFirst(Search search)
        {
            var code = @"
namespace N
{
    using System;

    internal class C : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var method = semanticModel.GetDeclaredSymbol(syntaxTree.FindClassDeclaration("C"));
            Assert.AreEqual("N.C.Dispose()", DisposeMethod.FindFirst(method, compilation, search).ToString());
        }
    }
}
