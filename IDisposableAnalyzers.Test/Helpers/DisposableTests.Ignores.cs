namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableTests
    {
        public static class Ignores
        {
            [TestCase("File.OpenRead(fileName)")]
            [TestCase("true ? File.OpenRead(fileName) : (FileStream)null")]
            [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
            [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
            [TestCase("new List<FileStream> { File.OpenRead(fileName) }")]
            public static void AssignedToLocal(string statement)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("string.Format(\"{0}\", File.OpenRead(fileName))")]
            public static void ArgumentPassedTo(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
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
            public static void ArgumentAssignedToTempLocal(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("var temp = disposable")]
            [TestCase("var temp = true ? disposable : (IDisposable)null")]
            public static void ArgumentAssignedToTempLocalThatIsDisposed(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("var temp = disposable")]
            [TestCase("var temp = true ? disposable : (IDisposable)null")]
            public static void ArgumentAssignedTempLocalInUsing(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("File.OpenRead(fileName)")]
            [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
            [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
            [TestCase("new List<FileStream> { File.OpenRead(fileName) }")]
            [TestCase("new FileStream [] { File.OpenRead(fileName) }")]
            public static void AssignedToField(string statement)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Task.Run(() => File.OpenRead(fileName))")]
            [TestCase("Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(false)")]
            [TestCase("Task.FromResult(File.OpenRead(fileName))")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(false)")]
            public static void AssignedToFieldAsync(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private readonly object value;

        public static async Task M(string fileName)
        {
            this.value = await Task.Run(() => File.OpenRead(fileName));
        }
    }
}".AssertReplace("Task.Run(() => File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("File.OpenRead(fileName)")]
            [TestCase("_ = File.OpenRead(fileName)")]
            [TestCase("var _ = File.OpenRead(fileName)")]
            [TestCase("M(File.OpenRead(fileName))")]
            [TestCase("new List<IDisposable> { File.OpenRead(fileName) }")]
            public static void Discarded(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("new C(File.OpenRead(fileName))")]
            [TestCase("_ = new C(File.OpenRead(fileName))")]
            [TestCase("var _ = new C(File.OpenRead(fileName))")]
            [TestCase("M(new C(File.OpenRead(fileName)))")]
            [TestCase("new List<IDisposable> { File.OpenRead(fileName) }")]
            [TestCase("_ = new List<IDisposable> { File.OpenRead(fileName) }")]
            [TestCase("new List<C> { new C(File.OpenRead(fileName)) }")]
            [TestCase("_ = new List<C> { new C(File.OpenRead(fileName)) }")]
            public static void DiscardedWrapped(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void ReturnedExpressionBody()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void ReturnedStatementBody()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void ReturnedDisposableCtorArg()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("new StreamReader(File.OpenRead(string.Empty))")]
            [TestCase("File.OpenRead(string.Empty).M2()")]
            [TestCase("File.OpenRead(string.Empty)?.M2()")]
            [TestCase("M2(File.OpenRead(string.Empty))")]
            public static void ReturnedStreamWrappedInStreamReader(string expression)
            {
                var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public StreamReader M1() => File.OpenRead(string.Empty).M2();

        private static StreamReader M2(this Stream stream) => new StreamReader(stream);
    }
}".AssertReplace("File.OpenRead(string.Empty).M2()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(string.Empty)");
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("await File.OpenRead(string.Empty).ReadAsync(null, 0, 0)")]
            [TestCase("await File.OpenRead(string.Empty)?.ReadAsync(null, 0, 0)")]
            [TestCase("File.OpenRead(string.Empty).ReadAsync(null, 0, 0)")]
            [TestCase("File.OpenRead(string.Empty)?.ReadAsync(null, 0, 0)")]
            public static void FileOpenReadReadAsync(string expression)
            {
                var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public async Task<int> M() => await File.OpenRead(string.Empty).ReadAsync(null, 0, 0);
    }
}".AssertReplace("await File.OpenRead(string.Empty).ReadAsync(null, 0, 0)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(string.Empty)");
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("new CompositeDisposable(File.OpenRead(fileName))")]
            [TestCase("new CompositeDisposable { File.OpenRead(fileName) }")]
            public static void ReturnedInCompositeDisposable(string expression)
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgAssignedNotDisposable()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgAssignedNotDisposableFactoryMethod()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgAssignedNotDisposed()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgAssignedNotDisposedFactoryMethod()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgNotAssigned()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgAssignedDisposed()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CtorArgAssignedNotAssigned()
            {
                var code = @"
namespace N
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
                Assert.AreEqual(true, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.AddAndReturn(File.OpenRead(fileName))")]
            [TestCase("disposable.AddAndReturn(File.OpenRead(fileName)).ToString()")]
            public static void CompositeDisposableExtAddAndReturn(string expression)
            {
                var code = @"
namespace N
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
            return this.disposable.AddAndReturn(File.OpenRead(fileName));
        }
    }
}".AssertReplace("disposable.AddAndReturn(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("File.OpenRead(fileName)")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).Result")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).GetAwaiter().GetResult()")]
            public static void UsingDeclaration(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class C
    {
        C(string fileName)
        {
            using var disposable = File.OpenRead(fileName);
        }
    }
}".AssertReplace("File.OpenRead(fileName)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Task.FromResult(File.OpenRead(fileName))")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(true)")]
            [TestCase("Task.Run(() => File.OpenRead(fileName))")]
            [TestCase("Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(true)")]
            public static void UsingDeclarationAwait(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class C
    {
        async Task M(string fileName)
        {
            using var disposable = await Task.FromResult(File.OpenRead(fileName));
        }
    }
}".AssertReplace("Task.FromResult(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Task.FromResult(File.OpenRead(fileName))")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(true)")]
            [TestCase("Task.Run(() => File.OpenRead(fileName))")]
            [TestCase("Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(true)")]
            public static void AssigningFieldAwait(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public async Task M(string fileName)
        {
            this.disposable?.Dispose();
            this.disposable = await Task.FromResult(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}".AssertReplace("Task.FromResult(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }

            [TestCase("leaveOpen: false", false)]
            [TestCase("leaveOpen: true", true)]
            public static void AsReadOnlyViewAsReadOnlyFilteredView(string expression, bool expected)
            {
                var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using Gu.Reactive;

    public static class C
    {
        public static ReadOnlyFilteredView<T> M<T>(
            this IObservable<IEnumerable<T>> source,
            Func<T, bool> filter)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return source.AsReadOnlyView().AsReadOnlyFilteredView(filter, leaveOpen: false);
        }
    }
}".AssertReplace("leaveOpen: false", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation("AsReadOnlyView()");
                Assert.AreEqual(expected, Disposable.Ignores(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
