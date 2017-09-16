namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using NUnit.Framework;

    public class DocumentEditorExtTests
    {
        [Test]
        public async Task AddPrivateMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public abstract class Foo
    {
        public int Filed1 = 1;
        private int filed1;

        public Foo()
        {
        }

        private Foo( int i)
        {
        }

        public int Prop1 { get; set; }

        public void Bar1()
        {
        }

        internal void Bar2()
        {
        }

        protected void Bar3()
        {
        }

        private static void Bar4()
        {
        }

        private void Bar5()
        {
        }
    }
}";
            var sln = CodeFactory.CreateSolution(testCode);
            var editor = await DocumentEditor.CreateAsync(sln.Projects.First().Documents.First()).ConfigureAwait(false);
            var containingType = editor.OriginalRoot.SyntaxTree.FindBestMatch<ClassDeclarationSyntax>("Foo");
            var method = new MethodTemplate("private int NewMethod() => 1;").MethodDeclarationSyntax;

            var expected = @"
namespace RoslynSandbox
{
    public abstract class Foo
    {
        public int Filed1 = 1;
        private int filed1;

        public Foo()
        {
        }

        private Foo( int i)
        {
        }

        public int Prop1 { get; set; }

        public void Bar1()
        {
        }

        internal void Bar2()
        {
        }

        protected void Bar3()
        {
        }

        private static void Bar4()
        {
        }

        private void Bar5()
        {
        }

        private int NewMethod() => 1;
    }
}";
            editor.AddSorted(containingType, method);
            CodeAssert.AreEqual(expected, editor.GetChangedDocument());
        }

        [Test]
        public async Task MakeSealed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int Filed1 = 1;
        protected int filed2;
        private int filed3;

        public Foo()
        {
        }

        protected Foo( int i)
        {
        }

        public virtual int Prop1 { get; set; }

        public virtual int Prop2 { get; protected set; }

        public virtual void Bar1()
        {
        }

        internal void Bar2()
        {
        }

        protected static void Bar3()
        {
        }

        protected void Bar4()
        {
        }

        private static void Bar5()
        {
        }

        private void Bar6()
        {
        }
    }
}";
            var sln = CodeFactory.CreateSolution(testCode);
            var editor = await DocumentEditor.CreateAsync(sln.Projects.First().Documents.First()).ConfigureAwait(false);
            var containingType = editor.OriginalRoot.SyntaxTree.FindBestMatch<ClassDeclarationSyntax>("Foo");
            var expected = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public int Filed1 = 1;
        private int filed2;
        private int filed3;

        public Foo()
        {
        }

        private Foo( int i)
        {
        }

        public int Prop1 { get; set; }

        public int Prop2 { get; private set; }

        public void Bar1()
        {
        }

        internal void Bar2()
        {
        }

        private static void Bar3()
        {
        }

        private void Bar4()
        {
        }

        private static void Bar5()
        {
        }

        private void Bar6()
        {
        }
    }
}";
            editor.MakeSealed(containingType);
            CodeAssert.AreEqual(expected, editor.GetChangedDocument());
        }
    }
}