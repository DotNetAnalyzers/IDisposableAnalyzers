namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableTests
    {
        public class IsIgnored
        {
            [Test]
            public void AssignedToLocal()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var value = File.OpenRead(fileName);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [TestCase("_ = File.OpenRead(fileName)")]
            [TestCase("File.OpenRead(fileName)")]
            [TestCase("var _ = File.OpenRead(fileName)")]
            public void Discarded(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            _ = File.OpenRead(fileName);
        }
    }
}".AssertReplace("_ = File.OpenRead(fileName)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
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
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
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
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
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
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
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
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void ReturnedNotDisposableCtorArg()
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
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void ReturnedNotDisposedCtorArg()
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
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void ReturnedNotAssignedCtorArg()
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
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void AssignedDisposedCtorArg()
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
            var foo = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void AssignedNotDisposableCtorArg()
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

        public static void M(string fileName)
        {
            var foo = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void AssignedNotDisposedCtorArg()
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

        public static void M(string fileName)
        {
            var foo = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void AssignedNotAssignedCtorArg()
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
            var foo = new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void CompositeDisposableExtAddAndReturn()
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

        internal string AddAndReturnToString(string fileName)
        {
            return disposable.AddAndReturn(File.OpenRead(fileName)).ToString();
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
            [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
            public void AssignedToTuple(string expression)
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
             var tuple = Tuple.Create(File.OpenRead(fileName), 1);
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(fileName), 1)", expression);

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
