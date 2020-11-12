#pragma warning disable GU0073 // Member of non-public type should be internal.
namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

#pragma warning disable GURA07 // Test class should be public static.
    internal static class ReturnValueWalkerTests
#pragma warning restore GURA07 // Test class should be public static.
    {
        [TestCase(ReturnValueSearch.Recursive, "")]
        [TestCase(ReturnValueSearch.Member, "await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
        public static void AwaitSyntaxError(ReturnValueSearch search, string expected)
        {
            var code = @"
using System.Threading.Tasks;

internal class C
{
    internal static async Task M()
    {
        var text = await CreateAsync().ConfigureAwait(false);
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false);
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression("await CreateAsync().ConfigureAwait(false)");
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("this.CalculatedExpressionBody", ReturnValueSearch.Recursive, "1")]
        [TestCase("this.CalculatedExpressionBody", ReturnValueSearch.Member, "1")]
        [TestCase("this.CalculatedStatementBody", ReturnValueSearch.Recursive, "1")]
        [TestCase("this.CalculatedStatementBody", ReturnValueSearch.Member, "1")]
        [TestCase("this.ThisExpressionBody", ReturnValueSearch.Recursive, "this")]
        [TestCase("this.ThisExpressionBody", ReturnValueSearch.Member, "this")]
        [TestCase("this.CalculatedReturningFieldExpressionBody", ReturnValueSearch.Recursive, "this.value")]
        [TestCase("this.CalculatedReturningFieldExpressionBody", ReturnValueSearch.Member, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody", ReturnValueSearch.Recursive, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody", ReturnValueSearch.Member, "this.value")]
        public static void Property(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    internal class C
    {
        private readonly int value = 1;

        internal C()
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

        public C ThisExpressionBody => this;

        public int CalculatedReturningFieldExpressionBody => this.value;

        public int CalculatedReturningFieldStatementBody
        {
            get
            {
                return this.value;
            }
        }
    }
}".AssertReplace("var temp = CalculatedExpressionBody", $"var temp = {expression}");
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("StaticRecursiveExpressionBody", ReturnValueSearch.Recursive, "")]
        [TestCase("StaticRecursiveExpressionBody", ReturnValueSearch.Member, "StaticRecursiveExpressionBody")]
        [TestCase("StaticRecursiveStatementBody", ReturnValueSearch.Recursive, "")]
        [TestCase("StaticRecursiveStatementBody", ReturnValueSearch.Member, "StaticRecursiveStatementBody")]
        [TestCase("RecursiveExpressionBody", ReturnValueSearch.Recursive, "")]
        [TestCase("RecursiveExpressionBody", ReturnValueSearch.Member, "this.RecursiveExpressionBody")]
        [TestCase("this.RecursiveExpressionBody", ReturnValueSearch.Recursive, "")]
        [TestCase("this.RecursiveExpressionBody", ReturnValueSearch.Member, "this.RecursiveExpressionBody")]
        [TestCase("this.RecursiveStatementBody", ReturnValueSearch.Recursive, "")]
        [TestCase("this.RecursiveStatementBody", ReturnValueSearch.Member, "this.RecursiveStatementBody")]
        [TestCase("RecursiveStatementBody", ReturnValueSearch.Recursive, "")]
        [TestCase("RecursiveStatementBody", ReturnValueSearch.Member, "this.RecursiveStatementBody")]
        public static void PropertyRecursive(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    internal class C
    {
        internal C()
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
}".AssertReplace("var temp = StaticRecursiveExpressionBody", $"var temp = {expression}");
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("StaticCreateIntStatementBody()", ReturnValueSearch.Recursive, "1")]
        [TestCase("StaticCreateIntStatementBody()", ReturnValueSearch.Member, "1")]
        [TestCase("StaticCreateIntExpressionBody()", ReturnValueSearch.Recursive, "2")]
        [TestCase("StaticCreateIntExpressionBody()", ReturnValueSearch.Member, "2")]
        [TestCase("IdStatementBody(1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("IdStatementBody(1)", ReturnValueSearch.Member, "1")]
        [TestCase("IdExpressionBody(1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("IdExpressionBody(1)", ReturnValueSearch.Member, "1")]
        [TestCase("OptionalIdExpressionBody()", ReturnValueSearch.Recursive, "1")]
        [TestCase("OptionalIdExpressionBody()", ReturnValueSearch.Member, "1")]
        [TestCase("OptionalIdExpressionBody(1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("OptionalIdExpressionBody(1)", ReturnValueSearch.Member, "1")]
        [TestCase("AssigningToParameter(1)", ReturnValueSearch.Recursive, "1, 2, 3, 4")]
        [TestCase("AssigningToParameter(1)", ReturnValueSearch.Member, "1, 4")]
        [TestCase("CallingIdExpressionBody(1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("CallingIdExpressionBody(1)", ReturnValueSearch.RecursiveInside, "")]
        [TestCase("CallingIdExpressionBody(1)", ReturnValueSearch.Member, "IdExpressionBody(arg1)")]
        [TestCase("ReturnLocal()", ReturnValueSearch.Recursive, "1")]
        [TestCase("ReturnLocal()", ReturnValueSearch.Member, "local")]
        [TestCase("ReturnLocalAssignedTwice(true)", ReturnValueSearch.Recursive, "1, 2, 3")]
        [TestCase("ReturnLocalAssignedTwice(true)", ReturnValueSearch.Member, "local, 3")]
        [TestCase("System.Threading.Tasks.Task.Run(() => 1)", ReturnValueSearch.Recursive, "")]
        [TestCase("System.Threading.Tasks.Task.Run(() => 1)", ReturnValueSearch.Member, "")]
        [TestCase("Missing()", ReturnValueSearch.Recursive, "")]
        [TestCase("Missing()", ReturnValueSearch.Member, "")]
        [TestCase("this.ThisExpressionBody()", ReturnValueSearch.Recursive, "this")]
        [TestCase("this.ThisExpressionBody()", ReturnValueSearch.Member, "this")]
        [TestCase("ReturningFileOpenRead()", ReturnValueSearch.Recursive, "System.IO.File.OpenRead(string.Empty)")]
        [TestCase("ReturningFileOpenRead()", ReturnValueSearch.Member, "System.IO.File.OpenRead(string.Empty)")]
        [TestCase("ReturningLocalFileOpenRead()", ReturnValueSearch.Recursive, "System.IO.File.OpenRead(string.Empty)")]
        [TestCase("ReturningLocalFileOpenRead()", ReturnValueSearch.Member, "stream")]
        public static void Call(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class C
    {
        internal C()
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

        public C ThisExpressionBody() => this;

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
}".AssertReplace("var temp = StaticCreateIntStatementBody()", $"var temp = {expression}");
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("Recursive()", ReturnValueSearch.Recursive, "")]
        [TestCase("Recursive()", ReturnValueSearch.Member, "Recursive()")]
        [TestCase("Recursive(1)", ReturnValueSearch.Recursive, "")]
        [TestCase("Recursive(1)", ReturnValueSearch.Member, "Recursive(arg)")]
        [TestCase("Recursive1(1)", ReturnValueSearch.Recursive, "")]
        [TestCase("Recursive1(1)", ReturnValueSearch.Member, "Recursive2(value)")]
        [TestCase("Recursive2(1)", ReturnValueSearch.Recursive, "")]
        [TestCase("Recursive2(1)", ReturnValueSearch.Member, "Recursive1(value)")]
        [TestCase("Recursive(true)", ReturnValueSearch.Recursive, "!flag, true")]
        [TestCase("Recursive(true)", ReturnValueSearch.Member, "Recursive(!flag), true")]
        [TestCase("RecursiveWithOptional(1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1)", ReturnValueSearch.Member, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, null)", ReturnValueSearch.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1, null)", ReturnValueSearch.Member, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", ReturnValueSearch.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", ReturnValueSearch.Member, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("Flatten(null, null)", ReturnValueSearch.Member, "null")]
        [TestCase("Flatten(null, null)", ReturnValueSearch.Recursive, "null, new List<IDisposable>()")]
        public static void CallRecursive(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class C
    {
        internal C()
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

        private static IReadOnlyList<IDisposable> Flatten(IReadOnlyList<IDisposable> source, List<IDisposable> result = null)
        {
            result = result ?? new List<IDisposable>();
            result.AddRange(source);
            foreach (var condition in source)
            {
                Flatten(new[] { condition }, result);
            }

            return result;
        }
    }
}".AssertReplace("var temp = Recursive()", $"var temp = {expression}");
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [Test]
        public static void RecursiveWithOptionalParameter()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public abstract class C
    {
        public C(IDisposable disposable)
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
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation("WithOptionalParameter(local)");
            using var walker = ReturnValueWalker.Borrow(value, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
            Assert.AreEqual("disposable", string.Join(", ", walker.Values));
        }

        [TestCase("Func<int> temp = () => 1", ReturnValueSearch.Recursive, "1")]
        [TestCase("Func<int> temp = () => 1", ReturnValueSearch.Member, "1")]
        [TestCase("Func<int, int> temp = x => 1", ReturnValueSearch.Recursive, "1")]
        [TestCase("Func<int, int> temp = x => 1", ReturnValueSearch.Member, "1")]
        [TestCase("Func<int, int> temp = x => x", ReturnValueSearch.Recursive, "x")]
        [TestCase("Func<int, int> temp = x => x", ReturnValueSearch.Member, "x")]
        [TestCase("Func<int> temp = () => { return 1; }", ReturnValueSearch.Recursive, "1")]
        [TestCase("Func<int> temp = () => { return 1; }", ReturnValueSearch.Member, "1")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", ReturnValueSearch.Recursive, "1, 2")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", ReturnValueSearch.Member, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", ReturnValueSearch.Recursive, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", ReturnValueSearch.Member, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", ReturnValueSearch.Recursive, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", ReturnValueSearch.Member, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", ReturnValueSearch.Recursive, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", ReturnValueSearch.Member, "1, 2")]
        public static void Lambda(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C()
        {
            Func<int> temp = () => 1;
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }
    }
}".AssertReplace("Func<int> temp = () => 1", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("await CreateAsync(0)", ReturnValueSearch.Recursive, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0)", ReturnValueSearch.Member, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", ReturnValueSearch.Recursive, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", ReturnValueSearch.Member, "1, 0, 2, 3")]
        [TestCase("await CreateStringAsync()", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await CreateStringAsync()", ReturnValueSearch.Member, "new string(' ', 1)")]
        [TestCase("await CreateIntAsync()", ReturnValueSearch.Recursive, "1")]
        [TestCase("await CreateIntAsync()", ReturnValueSearch.Member, "1")]
        [TestCase("await CreateIntAsync().ConfigureAwait(false)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await CreateIntAsync().ConfigureAwait(false)", ReturnValueSearch.Member, "1")]
        [TestCase("await ReturnTaskFromResultAsync()", ReturnValueSearch.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync()", ReturnValueSearch.Member, "1")]
        [TestCase("await ReturnTaskFromResultAsync().ConfigureAwait(false)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync().ConfigureAwait(false)", ReturnValueSearch.Member, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1)", ReturnValueSearch.Member, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1).ConfigureAwait(false)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await ReturnTaskFromResultAsync(1).ConfigureAwait(false)", ReturnValueSearch.Member, "1")]
        [TestCase("await ReturnAwaitTaskRunAsync()", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync()", ReturnValueSearch.Member, "await Task.Run(() => new string(\' \', 1))")]
        [TestCase("await ReturnAwaitTaskRunAsync().ConfigureAwait(false)", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync().ConfigureAwait(false)", ReturnValueSearch.Member, "await Task.Run(() => new string(' ', 1))")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync()", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync()", ReturnValueSearch.Member, "await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync().ConfigureAwait(false)", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunConfigureAwaitAsync().ConfigureAwait(false)", ReturnValueSearch.Member, "await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)")]
        [TestCase("await Task.Run(() => 1)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await Task.Run(() => 1)", ReturnValueSearch.Member, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", ReturnValueSearch.Member, "1")]
        [TestCase("await Task.Run(() => new Disposable())", ReturnValueSearch.Recursive, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable())", ReturnValueSearch.Member, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", ReturnValueSearch.Recursive, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", ReturnValueSearch.Member, "new Disposable()")]
        [TestCase("await Task.Run(() => new string(' ', 1))", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1))", ReturnValueSearch.Member, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", ReturnValueSearch.Member, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => CreateInt())", ReturnValueSearch.Recursive, "1")]
        [TestCase("await Task.Run(() => CreateInt())", ReturnValueSearch.Member, "CreateInt()")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", ReturnValueSearch.Member, "CreateInt()")]
        [TestCase("await Task.FromResult(new string(' ', 1))", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1))", ReturnValueSearch.Member, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", ReturnValueSearch.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", ReturnValueSearch.Member, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(CreateInt())", ReturnValueSearch.Recursive, "1")]
        [TestCase("await Task.FromResult(CreateInt())", ReturnValueSearch.Member, "CreateInt()")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", ReturnValueSearch.Recursive, "1")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", ReturnValueSearch.Member, "CreateInt()")]
        public static void AsyncAwait(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal async Task M()
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
}".AssertReplace("var value = await CreateStringAsync()", $"var value = {expression}");

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [TestCase("await RecursiveAsync()", ReturnValueSearch.Recursive, "")]
        [TestCase("await RecursiveAsync()", ReturnValueSearch.Member, "RecursiveAsync()")]
        [TestCase("await RecursiveAsync(1)", ReturnValueSearch.Recursive, "")]
        [TestCase("await RecursiveAsync(1)", ReturnValueSearch.Member, "RecursiveAsync(arg)")]
        [TestCase("await RecursiveAsync1(1)", ReturnValueSearch.Recursive, "")]
        [TestCase("await RecursiveAsync1(1)", ReturnValueSearch.Member, "await RecursiveAsync2(value)")]
        [TestCase("await RecursiveAsync3(1)", ReturnValueSearch.Recursive, "")]
        [TestCase("await RecursiveAsync3(1)", ReturnValueSearch.Member, "RecursiveAsync4(value)")]
        public static void AsyncAwaitRecursive(string expression, ReturnValueSearch search, string expected)
        {
            var code = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal async Task M()
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
}".AssertReplace("var value = await RecursiveAsync()", $"var value = {expression}");

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            using var walker = ReturnValueWalker.Borrow(value, search, semanticModel, CancellationToken.None);
            Assert.AreEqual(expected, string.Join(", ", walker.Values));
        }

        [Test]
        public static void ChainedExtensionMethod()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(int i)
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
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindEqualsValueClause("var value = i.AsDisposable().AsDisposable()").Value;
            using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
            Assert.AreEqual("new WrappingDisposable(d)", walker.Values.Single().ToString());
        }

        [Test]
        public static void ReturnTernary()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
            var temp = ReturnTernary(true);
        }

        private static int ReturnTernary(bool b)
        {
            return b ? 1 : 2;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindEqualsValueClause("var temp = ReturnTernary(true)").Value;
            using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
            Assert.AreEqual("1, 2", string.Join(", ", walker.Values));
        }

        [Test]
        public static void ReturnNullCoalesce()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
            var temp = ReturnNullCoalesce(null);
        }

        private static string ReturnNullCoalesce(string text)
        {
            return text ?? string.Empty;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindEqualsValueClause("var temp = ReturnNullCoalesce(null)").Value;
            using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
            Assert.AreEqual("null, string.Empty", string.Join(", ", walker.Values));
        }

        [Test]
        public static void ValidationErrorToStringConverter()
        {
            var code = @"
namespace N
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
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var methodDeclaration = syntaxTree.FindMethodDeclaration("Convert");
            using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
            Assert.AreEqual("error.ErrorContent, result.ErrorContent, value", string.Join(", ", walker.Values));
        }
    }
}
