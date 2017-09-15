namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class DisposableTests
    {
        internal class IsCreation
        {
            [TestCase("1", Result.No)]
            [TestCase("new string(' ', 1)", Result.No)]
            [TestCase("new Disposable()", Result.Yes)]
            [TestCase("new Disposable() as object", Result.Yes)]
            [TestCase("(object) new Disposable()", Result.Yes)]
            [TestCase("typeof(IDisposable)", Result.No)]
            [TestCase("(IDisposable)null", Result.No)]
            [TestCase("File.OpenRead(string.Empty) ?? null", Result.Yes)]
            [TestCase("null ?? File.OpenRead(string.Empty)", Result.Yes)]
            [TestCase("true ? null : File.OpenRead(string.Empty)", Result.Yes)]
            [TestCase("true ? File.OpenRead(string.Empty) : null", Result.Yes)]
            public void LanguageConstructs(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal Foo()
        {
            var value = new Disposable();
        }
    }
}";
                testCode = testCode.AssertReplace("new Disposable()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("new List<IDisposable>().Find(x => true)", Result.No)]
            [TestCase("ImmutableList<IDisposable>.Empty.Find(x => true)", Result.No)]
            [TestCase("new Queue<IDisposable>().Peek()", Result.No)]
            [TestCase("ImmutableQueue<IDisposable>.Empty.Peek()", Result.No)]
            [TestCase("new List<IDisposable>()[0]", Result.No)]
            [TestCase("ImmutableList<IDisposable>.Empty[0]", Result.Unknown)]
            public void Ignored(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal Foo()
        {
            var value = new Disposable();
        }
    }
}";
                testCode = testCode.AssertReplace("new Disposable()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("StaticCreateIntStatementBody()", Result.No)]
            [TestCase("StaticCreateIntExpressionBody()", Result.No)]
            [TestCase("StaticCreateIntWithArg()", Result.No)]
            [TestCase("StaticCreateIntId()", Result.No)]
            [TestCase("StaticCreateIntSquare()", Result.No)]
            [TestCase("this.CreateIntStatementBody()", Result.No)]
            [TestCase("CreateIntExpressionBody()", Result.No)]
            [TestCase("CreateIntWithArg()", Result.No)]
            [TestCase("CreateIntId()", Result.No)]
            [TestCase("CreateIntSquare()", Result.No)]
            [TestCase("Id<int>()", Result.No)]
            public void CallMethodInSyntaxTreeReturningNotDisposable(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal Foo()
        {
            // Meh();
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }

        internal static int StaticCreateIntExpressionBody() => 2;

        internal static int StaticCreateIntWithArg(int arg) => 3;

        internal static int StaticCreateIntId(int arg) => arg;

        internal static int StaticCreateIntSquare(int arg) => arg * arg;

        internal int CreateIntStatementBody()
        {
            return 1;
        }

        internal int CreateIntExpressionBody() => 2;

        internal int CreateIntWithArg(int arg) => 3;
   
        internal int CreateIntId(int arg) => arg;
   
        internal int CreateIntSquare(int arg) => arg * arg;

        internal T Id<T>(T arg) => arg;
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<InvocationExpressionSyntax>(code);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Id<IDisposable>(null)", Result.No)]
            [TestCase("this.Id<IDisposable>(null)", Result.No)]
            [TestCase("this.Id<IDisposable>(this.disposable)", Result.No)]
            [TestCase("this.Id<IDisposable>(new Disposable())", Result.Yes)]
            [TestCase("this.Id<object>(new Disposable())", Result.Yes)]
            [TestCase("CreateDisposableStatementBody()", Result.Yes)]
            [TestCase("this.CreateDisposableStatementBody()", Result.Yes)]
            [TestCase("CreateDisposableExpressionBody()", Result.Yes)]
            [TestCase("CreateDisposableExpressionBodyReturnTypeObject()", Result.Yes)]
            [TestCase("CreateDisposableInIf(true)", Result.Yes)]
            [TestCase("CreateDisposableInElse(true)", Result.Yes)]
            [TestCase("ReturningLocal()", Result.Yes)]
            public void CallMethodInSyntaxTreeReturningDisposable(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        private readonly IDisposable disposable = new Disposable();

        internal Foo()
        {
            // Meh();
        }

        internal T Id<T>(T arg) => arg;

        internal T ConstrainedFactory<T>(T arg) where T : IDisposable, new() => new T();

        internal T ConstrainedStructFactory<T>(T arg) where T : struct, new() => new T();

        internal IDisposable CreateDisposableStatementBody()
        {
            return new Disposable();
        }

        internal IDisposable CreateDisposableExpressionBody() => new Disposable();
       
        internal object CreateDisposableExpressionBodyReturnTypeObject() => new Disposable();

        internal IDisposable CreateDisposableInIf(bool flag)
        {
            if (flag)
            {
                return new Disposable();
            }
            else
            {
                return null;
            }

            return null;
        }

        internal IDisposable CreateDisposableInElse(bool flag)
        {
            if (flag)
            {
                return null;
            }
            else
            {
                return new Disposable();
            }

            return null;
        }

        public static Stream ReturningLocal()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<InvocationExpressionSyntax>(code);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("StaticRecursiveStatementBody()", Result.No)]
            [TestCase("StaticRecursiveExpressionBody()", Result.No)]
            [TestCase("CallingRecursive()", Result.No)]
            [TestCase("RecursiveTernary(true)", Result.Yes)]
            [TestCase("this.RecursiveExpressionBody()", Result.No)]
            [TestCase("this.RecursiveStatementBody()", Result.No)]
            public void CallRecursiveMethod(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal Foo()
        {
            // Meh();
        }

        private static IDisposable StaticRecursiveStatementBody()
        {
            return StaticRecursiveStatementBody();
        }

        private static IDisposable StaticRecursiveExpressionBody() => StaticRecursiveExpressionBody();

        private static IDisposable CallingRecursive() => StaticRecursiveStatementBody();

        private static IDisposable RecursiveTernary(bool flag) => flag ? new Disposable() : RecursiveTernary(bool flag);

        private IDisposable RecursiveStatementBody()
        {
            return this.RecursiveStatementBody();
        }

        private IDisposable RecursiveExpressionBody() => this.RecursiveExpressionBody();
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<InvocationExpressionSyntax>(code);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Task.Run(() => 1)", Result.No)]
            [TestCase("Task.Run(() => new Disposable())", Result.No)]
            [TestCase("CreateStringAsync()", Result.No)]
            [TestCase("await CreateStringAsync()", Result.No)]
            [TestCase("await Task.Run(() => new string(' ', 1))", Result.No)]
            [TestCase("await Task.FromResult(new string(' ', 1))", Result.No)]
            [TestCase("await Task.Run(() => new Disposable())", Result.Yes)]
            [TestCase("await Task.FromResult(new Disposable())", Result.Yes)]
            [TestCase("await CreateDisposableAsync()", Result.Yes)]
            public void AsyncAwait(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal async Task Bar()
        {
            var value = // Meh();
        }

        internal static async Task<string> CreateStringAsync()
        {
            await Task.Delay(0);
            return new string(' ', 1);
        }

        internal static async Task<IDisposable> CreateDisposableAsync()
        {
            await Task.Delay(0);
            return new Disposable();
        }
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("File.OpenRead(string.Empty)", Result.Yes)]
            [TestCase("this.ints.GetEnumerator()", Result.Yes)]
            [TestCase("((IList)this.ints).GetEnumerator()", Result.No)]
            [TestCase("this.disposables.First()", Result.No)]
            [TestCase("this.disposableMap[1]", Result.No)]
            [TestCase("this.readonlyDisposableMap[1]", Result.No)]
            [TestCase("this.conditionalWeakTable.GetOrCreateValue(this.disposable)", Result.No)]
            [TestCase("new PasswordBox().SecurePassword", Result.Yes)]
            [TestCase("passwordBox.SecurePassword", Result.Yes)]
            [TestCase("PasswordBox.SecurePassword", Result.Yes)]
            public void CallExternal(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows.Controls;

    internal class Foo
    {
        private static readonly PasswordBox PasswordBox = new PasswordBox();

        private readonly IDisposable disposable = new Disposable();
        private readonly List<int> ints = new List<int>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();
      
        private readonly IReadOnlyDictionary<int, IDisposable> readonlyDisposableMap = new Dictionary<int, IDisposable>();
        private readonly Dictionary<int, IDisposable> disposableMap = new Dictionary<int, IDisposable>();
        private readonly ConditionalWeakTable<IDisposable, IDisposable> conditionalWeakTable = new ConditionalWeakTable<IDisposable, IDisposable>();

        private readonly PasswordBox passwordBox = new PasswordBox();

        internal Foo()
        {
            var value = // Meh();
        }
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
