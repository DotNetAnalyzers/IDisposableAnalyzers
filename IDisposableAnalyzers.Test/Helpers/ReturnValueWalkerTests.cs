namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal class ReturnValueWalkerTests
    {
        [TestCase(Search.Recursive, "")]
        [TestCase(Search.TopLevel, "await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
        public void AwaitSyntaxError(Search search, string expected)
        {
            var testCode = @"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync().ConfigureAwait(false);
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false);
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause("var text = await CreateAsync()").Value;
            using (var pooled = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("this.CalculatedExpressionBody", Search.Recursive, "1")]
        [TestCase("this.CalculatedExpressionBody", Search.TopLevel, "1")]
        [TestCase("this.CalculatedStatementBody", Search.Recursive, "1")]
        [TestCase("this.CalculatedStatementBody", Search.TopLevel, "1")]
        [TestCase("this.ThisExpressionBody", Search.Recursive, "this")]
        [TestCase("this.ThisExpressionBody", Search.TopLevel, "this")]
        [TestCase("this.CalculatedReturningFieldExpressionBody", Search.Recursive, "this.value")]
        [TestCase("this.CalculatedReturningFieldExpressionBody", Search.TopLevel, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody", Search.Recursive, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody", Search.TopLevel, "this.value")]
        public void Property(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value = 1;

        internal Foo()
        {
            var temp = CalculatedExpressionBody;
        }

        public int CalculatedExpressionBody => 1;

        public int CalculatedStatementBody
        {
            get
            {
                return 1;
            }
        }

        public Foo ThisExpressionBody => this;

        public int CalculatedReturningFieldExpressionBody => this.value;

        public int CalculatedReturningFieldStatementBody
        {
            get
            {
                return this.value;
            }
        }
    }
}";
            testCode = testCode.AssertReplace("var temp = CalculatedExpressionBody", $"var temp = {code}");
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", returnValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("StaticRecursiveExpressionBody", Search.Recursive, "")]
        [TestCase("StaticRecursiveExpressionBody", Search.TopLevel, "StaticRecursiveExpressionBody")]
        [TestCase("StaticRecursiveStatementBody", Search.Recursive, "")]
        [TestCase("StaticRecursiveStatementBody", Search.TopLevel, "StaticRecursiveStatementBody")]
        [TestCase("RecursiveExpressionBody", Search.Recursive, "")]
        [TestCase("RecursiveExpressionBody", Search.TopLevel, "this.RecursiveExpressionBody")]
        [TestCase("this.RecursiveExpressionBody", Search.Recursive, "")]
        [TestCase("this.RecursiveExpressionBody", Search.TopLevel, "this.RecursiveExpressionBody")]
        [TestCase("this.RecursiveStatementBody", Search.Recursive, "")]
        [TestCase("this.RecursiveStatementBody", Search.TopLevel, "this.RecursiveStatementBody")]
        [TestCase("RecursiveStatementBody", Search.Recursive, "")]
        [TestCase("RecursiveStatementBody", Search.TopLevel, "this.RecursiveStatementBody")]
        public void PropertyRecursive(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = StaticRecursiveExpressionBody;
        }

        public static int StaticRecursiveExpressionBody => StaticRecursiveExpressionBody;

        public static int StaticRecursiveStatementBody
        {
            get
            {
                return StaticRecursiveStatementBody;
            }
        }

        public int RecursiveExpressionBody => this.RecursiveExpressionBody;

        public int RecursiveStatementBody
        {
            get
            {
                return this.RecursiveStatementBody;
            }
        }
    }
}";
            testCode = testCode.AssertReplace("var temp = StaticRecursiveExpressionBody", $"var temp = {code}");
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", returnValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("StaticCreateIntStatementBody()", Search.Recursive, "1")]
        [TestCase("StaticCreateIntStatementBody()", Search.TopLevel, "1")]
        [TestCase("StaticCreateIntExpressionBody()", Search.Recursive, "2")]
        [TestCase("StaticCreateIntExpressionBody()", Search.TopLevel, "2")]
        [TestCase("IdStatementBody(1)", Search.Recursive, "1")]
        [TestCase("IdStatementBody(1)", Search.TopLevel, "1")]
        [TestCase("IdExpressionBody(1)", Search.Recursive, "1")]
        [TestCase("IdExpressionBody(1)", Search.TopLevel, "1")]
        [TestCase("OptionalIdExpressionBody()", Search.Recursive, "1")]
        [TestCase("OptionalIdExpressionBody()", Search.TopLevel, "1")]
        [TestCase("OptionalIdExpressionBody(1)", Search.Recursive, "1")]
        [TestCase("OptionalIdExpressionBody(1)", Search.TopLevel, "1")]
        [TestCase("AssigningToParameter(1)", Search.Recursive, "1, 2, 3, 4")]
        [TestCase("AssigningToParameter(1)", Search.TopLevel, "1, 4")]
        [TestCase("CallingIdExpressionBody(1)", Search.Recursive, "1")]
        [TestCase("CallingIdExpressionBody(1)", Search.TopLevel, "IdExpressionBody(arg1)")]
        [TestCase("ReturnLocal()", Search.Recursive, "1")]
        [TestCase("ReturnLocal()", Search.TopLevel, "local")]
        [TestCase("ReturnLocalAssignedTwice(true)", Search.Recursive, "1, 2, 3")]
        [TestCase("ReturnLocalAssignedTwice(true)", Search.TopLevel, "local, 3")]
        [TestCase("System.Threading.Tasks.Task.Run(() => 1)", Search.Recursive, "")]
        [TestCase("System.Threading.Tasks.Task.Run(() => 1)", Search.TopLevel, "")]
        [TestCase("Missing()", Search.Recursive, "")]
        [TestCase("Missing()", Search.TopLevel, "")]
        [TestCase("this.ThisExpressionBody()", Search.Recursive, "this")]
        [TestCase("this.ThisExpressionBody()", Search.TopLevel, "this")]
        [TestCase("this.ReturningFileOpenRead()", Search.Recursive, "System.IO.File.OpenRead(string.Empty)")]
        [TestCase("this.ReturningFileOpenRead()", Search.TopLevel, "System.IO.File.OpenRead(string.Empty)")]
        [TestCase("this.ReturningLocalFileOpenRead()", Search.Recursive, "System.IO.File.OpenRead(string.Empty)")]
        [TestCase("this.ReturningLocalFileOpenRead()", Search.TopLevel, "stream")]
        public void Call(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class Foo
    {
        internal Foo()
        {
            var temp = StaticCreateIntStatementBody();
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }

        internal static int StaticCreateIntExpressionBody() => 2;

        internal static int IdStatementBody(int arg)
        {
            return arg;
        }

        internal static int IdExpressionBody(int arg) => arg;

        internal static int OptionalIdExpressionBody(int arg = 1) => arg;

        internal static int CallingIdExpressionBody(int arg1) => IdExpressionBody(arg1);

        public static int AssigningToParameter(int arg)
        {
            if (true)
            {
                return arg;
            }
            else
            {
                if (true)
                {
                    arg = 2;
                }
                else
                {
                    arg = 3;
                }

                return arg;
            }

            return 4;
        }

        public static int ConditionalId(int arg)
        {
            if (true)
            {
                return arg;
            }

            return arg;
        }

        public static int ReturnLocal()
        {
            var local = 1;
            return local;
        }

        public static int ReturnLocalAssignedTwice(bool flag)
        {
            var local = 1;
            local = 2;
            if (flag)
            {
                return local;
            }

            local = 5;
            return 3;
        }

        public Foo ThisExpressionBody() => this;

        public static Stream ReturningFileOpenRead()
        {
            return System.IO.File.OpenRead(string.Empty);
        }

        public static Stream ReturningLocalFileOpenRead()
        {
            var stream = System.IO.File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
            testCode = testCode.AssertReplace("var temp = StaticCreateIntStatementBody()", $"var temp = {code}");
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", returnValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("Recursive()", Search.Recursive, "")]
        [TestCase("Recursive()", Search.TopLevel, "Recursive()")]
        [TestCase("Recursive(1)", Search.Recursive, "")]
        [TestCase("Recursive(1)", Search.TopLevel, "Recursive(arg)")]
        [TestCase("Recursive1(1)", Search.Recursive, "")]
        [TestCase("Recursive1(1)", Search.TopLevel, "Recursive2(value)")]
        [TestCase("Recursive2(1)", Search.Recursive, "")]
        [TestCase("Recursive2(1)", Search.TopLevel, "Recursive1(value)")]
        [TestCase("Recursive(true)", Search.Recursive, "!flag, true")]
        [TestCase("Recursive(true)", Search.TopLevel, "Recursive(!flag), true")]
        [TestCase("RecursiveWithOptional(1)", Search.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1)", Search.TopLevel, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, null)", Search.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1, null)", Search.TopLevel, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", Search.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", Search.TopLevel, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        public void CallRecursive(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class Foo
    {
        internal Foo()
        {
            var temp = Recursive();
        }

        public static int Recursive() => Recursive();

        public static int Recursive(int arg) => Recursive(arg);

        public static bool Recursive(bool flag)
        {
            if (flag)
            {
                return Recursive(!flag);
            }

            return flag;
        }

        private static int RecursiveWithOptional(int arg, IEnumerable<int> args = null)
        {
            if (arg == null)
            {
                return RecursiveWithOptional(arg, new[] { arg });
            }

            return arg;
        }

        private static int Recursive1(int value)
        {
            return Recursive2(value);
        }

        private static int Recursive2(int value)
        {
            return Recursive1(value);
        }
    }
}";
            testCode = testCode.AssertReplace("var temp = Recursive()", $"var temp = {code}");
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", returnValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void RecursiveWithOptionalParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public abstract class Foo
    {
        public Foo(IDisposable disposable)
        {
            var local = disposable;
            local = WithOptionalParameter(local);
        }

        private static IDisposable WithOptionalParameter(IDisposable parameter, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return WithOptionalParameter(parameter, new[] { parameter });
            }

            return parameter;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation("WithOptionalParameter(local)");
            using (var returnValues = ReturnValueWalker.Borrow(value, Search.Recursive, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", returnValues);
                Assert.AreEqual("disposable", actual);
            }
        }

        [TestCase("Func<int> temp = () => 1", Search.Recursive, "1")]
        [TestCase("Func<int> temp = () => 1", Search.TopLevel, "1")]
        [TestCase("Func<int, int> temp = x => 1", Search.Recursive, "1")]
        [TestCase("Func<int, int> temp = x => 1", Search.TopLevel, "1")]
        [TestCase("Func<int, int> temp = x => x", Search.Recursive, "x")]
        [TestCase("Func<int, int> temp = x => x", Search.TopLevel, "x")]
        [TestCase("Func<int> temp = () => { return 1; }", Search.Recursive, "1")]
        [TestCase("Func<int> temp = () => { return 1; }", Search.TopLevel, "1")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", Search.Recursive, "1, 2")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", Search.TopLevel, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", Search.Recursive, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", Search.TopLevel, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", Search.Recursive, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", Search.TopLevel, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", Search.Recursive, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", Search.TopLevel, "1, 2")]
        public void Lambda(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        internal Foo()
        {
            Func<int> temp = () => 1;
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }
    }
}";
            testCode = testCode.AssertReplace("Func<int> temp = () => 1", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", returnValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("await CreateAsync(0)", Search.Recursive, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0)", Search.TopLevel, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", Search.Recursive, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", Search.TopLevel, "1, 0, 2, 3")]
        [TestCase("await CreateStringAsync()", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await CreateStringAsync()", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await CreateIntAsync()", Search.Recursive, "1")]
        [TestCase("await CreateIntAsync()", Search.TopLevel, "1")]
        [TestCase("await CreateIntAsync().ConfigureAwait(false)", Search.Recursive, "1")]
        [TestCase("await CreateIntAsync().ConfigureAwait(false)", Search.TopLevel, "1")]
        [TestCase("await ReturnTaskFromResultAsync()", Search.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync()", Search.TopLevel, "1")]
        [TestCase("await ReturnTaskFromResultAsync().ConfigureAwait(false)", Search.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync().ConfigureAwait(false)", Search.TopLevel, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1)", Search.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1)", Search.TopLevel, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1).ConfigureAwait(false)", Search.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1).ConfigureAwait(false)", Search.TopLevel, "1")]
        [TestCase("await ReturnAwaitTaskRunAsync()", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync()", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync().ConfigureAwait(false)", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync().ConfigureAwait(false)", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync()", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync()", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync().ConfigureAwait(false)", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync().ConfigureAwait(false)", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => 1)", Search.Recursive, "1")]
        [TestCase("await Task.Run(() => 1)", Search.TopLevel, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", Search.Recursive, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", Search.TopLevel, "1")]
        [TestCase("await Task.Run(() => new Disposable())", Search.Recursive, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable())", Search.TopLevel, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", Search.Recursive, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", Search.TopLevel, "new Disposable()")]
        [TestCase("await Task.Run(() => new string(' ', 1))", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1))", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => CreateInt())", Search.Recursive, "1")]
        [TestCase("await Task.Run(() => CreateInt())", Search.TopLevel, "CreateInt()")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", Search.Recursive, "1")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", Search.TopLevel, "CreateInt()")]
        [TestCase("await Task.FromResult(new string(' ', 1))", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1))", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", Search.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", Search.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(CreateInt())", Search.Recursive, "1")]
        [TestCase("await Task.FromResult(CreateInt())", Search.TopLevel, "CreateInt()")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", Search.Recursive, "1")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", Search.TopLevel, "CreateInt()")]
        public void AsyncAwait(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
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
            var value = await CreateStringAsync();
        }

        internal static int CreateInt() => 1;

        internal static async Task<string> CreateStringAsync()
        {
            await Task.Delay(0);
            return new string(' ', 1);
        }

        internal static async Task<int> CreateIntAsync()
        {
            await Task.Delay(0);
            return 1;
        }

        internal static Task<int> ReturnTaskFromResultAsync() => Task.FromResult(1);

        internal static Task<int> ReturnTaskFromResultAsync(int arg) => Task.FromResult(arg);

        internal static async Task<string> ReturnAwaitTaskRunAsync() => await Task.Run(() => new string(' ', 1));

        internal static async Task<string> ReturnAwaitTaskRunConfigureAwaitAsync() => await Task.Run(() => new string(' ', 1)).ConfigureAwait(false);

        internal static Task<int> CreateAsync(int arg)
        {
            switch (arg)
            {
                case 0:
                    return Task.FromResult(1);
                case 1:
                    return Task.FromResult(arg);
                case 2:
                    return Task.Run(() => 2);
                case 3:
                    return Task.Run(() => arg);
                case 4:
                    return Task.Run(() => { return 3; });
                default:
                    return Task.Run(() => { return arg; });
            }
        }
    }
}";
            testCode = testCode.AssertReplace("var value = await CreateStringAsync()", $"var value = {code}");
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", returnValues));
            }
        }

        [TestCase("await RecursiveAsync()", Search.Recursive, "")]
        [TestCase("await RecursiveAsync()", Search.TopLevel, "RecursiveAsync()")]
        [TestCase("await RecursiveAsync(1)", Search.Recursive, "")]
        [TestCase("await RecursiveAsync(1)", Search.TopLevel, "RecursiveAsync(arg)")]
        [TestCase("await RecursiveAsync1(1)", Search.Recursive, "")]
        [TestCase("await RecursiveAsync1(1)", Search.TopLevel, "await RecursiveAsync2(value)")]
        [TestCase("await RecursiveAsync3(1)", Search.Recursive, "")]
        [TestCase("await RecursiveAsync3(1)", Search.TopLevel, "RecursiveAsync4(value)")]
        public void AsyncAwaitRecursive(string code, Search search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
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
            var value = await RecursiveAsync();
        }

        internal static Task<int> RecursiveAsync() => RecursiveAsync();

        internal static Task<int> RecursiveAsync(int arg) => RecursiveAsync(arg);

        private static async Task<int> RecursiveAsync1(int value)
        {
            return await RecursiveAsync2(value);
        }

        private static async Task<int> RecursiveAsync2(int value)
        {
            return await RecursiveAsync1(value);
        }

        private static Task<int> RecursiveAsync3(int value)
        {
            return RecursiveAsync4(value);
        }

        private static async Task<int> RecursiveAsync4(int value)
        {
            return await RecursiveAsync3(value);
        }
    }
}";
            testCode = testCode.AssertReplace("var value = await RecursiveAsync()", $"var value = {code}");
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            using (var returnValues = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", returnValues));
            }
        }

        [Test]
        public void ChainedExtensionMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar(int i)
        {
            var value = i.AsDisposable().AsDisposable();
        }
    }

    public static class Ext
    {
        public static IDisposable AsDisposable(this int i) => new Disposable();

        public static IDisposable AsDisposable(this IDisposable d) => new WrappingDisposable(d);
    }

    public sealed class WrappingDisposable : IDisposable
    {
        private readonly IDisposable inner;

        public WrappingDisposable(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
            this.inner.Dispose();
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindEqualsValueClause("var value = i.AsDisposable().AsDisposable()").Value;
            using (var returnValues = ReturnValueWalker.Borrow(methodDeclaration, Search.Recursive, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual("new WrappingDisposable(d)", returnValues.Single().ToString());
            }
        }

        [Test]
        public void ReturnTernary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var temp = ReturnTernary(true);
        }

        private static int ReturnTernary(bool b)
        {
            return b ? 1 : 2;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindEqualsValueClause("var temp = ReturnTernary(true)").Value;
            using (var returnValues = ReturnValueWalker.Borrow(methodDeclaration, Search.Recursive, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual("1, 2", string.Join(", ", returnValues));
            }
        }

        [Test]
        public void ReturnNullCoalesce()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var temp = ReturnNullCoalesce(null);
        }

        private static string ReturnNullCoalesce(string text)
        {
            return text ?? string.Empty;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindEqualsValueClause("var temp = ReturnNullCoalesce(null)").Value;
            using (var returnValues = ReturnValueWalker.Borrow(methodDeclaration, Search.Recursive, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual("null, string.Empty", string.Join(", ", returnValues));
            }
        }

        [Test]
        public void ValidationErrorToStringConverter()
        {
            var testCode = @"
namespace RoslynSandbox
{
     using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    public class ValidationErrorToStringConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        public static readonly ValidationErrorToStringConverter Default = new ValidationErrorToStringConverter();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text;
            }

            if (value is ValidationResult result)
            {
                return this.Convert(result.ErrorContent, targetType, parameter, culture);
            }

            if (value is ValidationError error)
            {
                return this.Convert(error.ErrorContent, targetType, parameter, culture);
            }

            return value;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($""{this.GetType().Name} only supports one-way conversion."");
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindMethodDeclaration("Convert");
            using (var returnValues = ReturnValueWalker.Borrow(methodDeclaration, Search.Recursive, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual("text, error.ErrorContent, result.ErrorContent, value", string.Join(", ", returnValues));
            }
        }
    }
}
