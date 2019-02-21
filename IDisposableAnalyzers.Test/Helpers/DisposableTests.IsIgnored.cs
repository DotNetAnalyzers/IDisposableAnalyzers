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
            public void ReturnedNotDisposableCtorArg()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }

    public class Meh
    {
        public C Bar(string fileName)
        {
            return new C(File.OpenRead(fileName));
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
            public void ReturnedNotDisposedCtorArg()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;

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

    public class Meh
    {
        public C Bar(string fileName)
        {
            return new C(File.OpenRead(fileName));
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
            public void ReturnedNotAssignedCtorArg()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
    {
        public C(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }

    public class Meh
    {
        public C Bar(string fileName)
        {
            return new C(File.OpenRead(fileName));
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

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }

    public class Meh
    {
        public void Bar(string fileName)
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
            public void AssignedNotDisposedCtorArg()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;

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

    public class Meh
    {
        public void Bar(string fileName)
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
            public void AssignedNotAssignedCtorArg()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
    {
        public C(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }

    public class Meh
    {
        public void Bar(string fileName)
        {
            var foo =  new C(File.OpenRead(fileName));
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
