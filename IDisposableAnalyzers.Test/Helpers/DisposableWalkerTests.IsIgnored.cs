namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableWalkerTests
    {
        public class IsIgnored
        {
            [TestCase("File.OpenRead(fileName)")]
            [TestCase("true ? File.OpenRead(fileName) : (FileStream)null")]
            [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
            [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
            [TestCase("new List<FileStream> { File.OpenRead(fileName) }")]
            public void AssignedToLocal(string statement)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            var value = File.OpenRead(fileName);
        }
    }
}".AssertReplace("File.OpenRead(fileName)", statement);

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("string.Format(\"{0}\", File.OpenRead(fileName))")]
            public void ArgumentPassedTo(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class C
    {
        public static object M() => string.Format(""{0}"", File.OpenRead(fileName));
    }
}".AssertReplace("string.Format(\"{0}\", File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("disposable")]
            ////[TestCase("disposable ?? disposable")]
            ////[TestCase("true ? disposable : (IDisposable)null")]
            ////[TestCase("Tuple.Create(disposable, 1)")]
            ////[TestCase("(disposable, 1)")]
            ////[TestCase("new Tuple<IDisposable, int>(disposable, 1)")]
            ////[TestCase("new List<IDisposable> { disposable }")]
            ////[TestCase("new List<IDisposable>() { disposable }")]
            ////[TestCase("new List<IDisposable> { disposable, null }")]
            public void ArgumentAssignedToTempLocal(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            var value = M(File.OpenRead(fileName));
        }

        public static int M(IDisposable disposable)
        {
            var temp = new List<IDisposable> { disposable };
            return 1;
        }
    }
}".AssertReplace("new List<IDisposable> { disposable }", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("var temp = disposable")]
            [TestCase("var temp = true ? disposable : (IDisposable)null")]
            public void ArgumentAssignedToTempLocalThatIsDisposed(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            var value = M(File.OpenRead(fileName));
        }

        public static int M(IDisposable disposable)
        {
            var temp = disposable;
            temp.Dispose();
            return 1;
        }
    }
}".AssertReplace("var temp = disposable", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("var temp = disposable")]
            [TestCase("var temp = true ? disposable : (IDisposable)null")]
            public void ArgumentAssignedTempLocalInUsing(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            var value = M(File.OpenRead(fileName));
        }

        public static int M(IDisposable disposable)
        {
            using (var temp = disposable)
            {
            }

            temp.Dispose();
            return 1;
        }
    }
}".AssertReplace("var temp = disposable", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("File.OpenRead(fileName)")]
            [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
            [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
            [TestCase("new List<FileStream> { File.OpenRead(fileName) }")]
            [TestCase("new FileStream [] { File.OpenRead(fileName) }")]
            public void AssignedToField(string statement)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private readonly object value;

        public static void M(string fileName)
        {
            this.value = File.OpenRead(fileName);
        }
    }
}".AssertReplace("File.OpenRead(fileName)", statement);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("File.OpenRead(fileName)")]
            [TestCase("_ = File.OpenRead(fileName)")]
            [TestCase("var _ = File.OpenRead(fileName)")]
            [TestCase("M(File.OpenRead(fileName))")]
            [TestCase("new List<IDisposable> { File.OpenRead(fileName) }")]
            public void Discarded(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            File.OpenRead(fileName);
        }

        public static void M(IDisposable) { }
    }
}".AssertReplace("File.OpenRead(fileName)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("new C(File.OpenRead(fileName))")]
            [TestCase("_ = new C(File.OpenRead(fileName))")]
            [TestCase("var _ = new C(File.OpenRead(fileName))")]
            [TestCase("M(new C(File.OpenRead(fileName)))")]
            [TestCase("new List<IDisposable> { File.OpenRead(fileName) }")]
            [TestCase("_ = new List<IDisposable> { File.OpenRead(fileName) }")]
            [TestCase("new List<C> { new C(File.OpenRead(fileName)) }")]
            [TestCase("_ = new List<C> { new C(File.OpenRead(fileName)) }")]
            public void DiscardedWrapped(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static void M(string fileName)
        {
            new C(File.OpenRead(fileName));
        }

        public static void M(IDisposable _) { }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}".AssertReplace("new C(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ReturnedExpressionBody()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        internal IDisposable C(string fileName) => File.OpenRead(fileName);
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ReturnedStatementBody()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        internal IDisposable C(string fileName)
        {
            return File.OpenRead(fileName);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ReturnedDisposableCtorArg()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static C Create(string fileName)
        {
            return new C(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("new CompositeDisposable(File.OpenRead(fileName))")]
            [TestCase("new CompositeDisposable { File.OpenRead(fileName) }")]
            public void ReturnedInCompositeDisposable(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public class C
    {
        public static IDisposable M(string fileName) => new CompositeDisposable(File.OpenRead(fileName));
    }
}".AssertReplace("new CompositeDisposable(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgAssignedNotDisposable()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }

    public static class C2
    {
        public static void M(string fileName)
        {
            var c = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgAssignedNotDisposableFactoryMethod()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static C Create(string fileName) => new C(File.OpenRead(fileName));
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgAssignedNotDisposed()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }

    public static class C2
    {
        public static void M(string fileName)
        {
            var c = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgAssignedNotDisposedFactoryMethod()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }

        public static C Create(string fileName) => new C(File.OpenRead(fileName));
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgNotAssigned()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        public C(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }

        public static C Create(string fileName)
        {
            return new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgAssignedDisposed()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        public static void M(string fileName)
        {
            var c = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void CtorArgAssignedNotAssigned()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        public C(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }

        public static void M(string fileName)
        {
            var c = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }

            [TestCase("disposable.AddAndReturn(File.OpenRead(fileName))")]
            [TestCase("disposable.AddAndReturn(File.OpenRead(fileName)).ToString()")]
            public void CompositeDisposableExtAddAndReturn(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        internal object M(string fileName)
        {
            return disposable.AddAndReturn(File.OpenRead(fileName));
        }
    }
}".AssertReplace("disposable.AddAndReturn(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, DisposableWalker.IsIgnored(value, semanticModel, CancellationToken.None, null));
            }
        }
    }
}
