#pragma warning disable GU0073 // Member of non-public type should be internal.
namespace IDisposableAnalyzers.Test.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    public static partial class DisposableTests
    {
        internal static class IsCreation
        {
            [TestCase("1",                                                   false)]
            [TestCase("new string(' ', 1)",                                  false)]
            [TestCase("new Disposable()",                                    true)]
            [TestCase("new Disposable() as object",                          true)]
            [TestCase("(object) new Disposable()",                           true)]
            [TestCase("typeof(IDisposable)",                                 false)]
            [TestCase("(IDisposable)null",                                   false)]
            [TestCase("System.IO.File.OpenRead(string.Empty) ?? null",       true)]
            [TestCase("null ?? System.IO.File.OpenRead(string.Empty)",       true)]
            [TestCase("true ? null : System.IO.File.OpenRead(string.Empty)", true)]
            [TestCase("true ? System.IO.File.OpenRead(string.Empty) : null", true)]
            public static void LanguageConstructs(string expression, bool expected)
            {
                var code = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            var value = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(expression).Value;
                Assert.AreEqual(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
                Assert.IsNotInstanceOf<IErrorTypeSymbol>(type);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("StaticCreateIntStatementBody()")]
            [TestCase("StaticCreateIntExpressionBody()")]
            [TestCase("StaticCreateIntWithArg()")]
            [TestCase("StaticCreateIntId()")]
            [TestCase("StaticCreateIntSquare()")]
            [TestCase("this.CreateIntStatementBody()")]
            [TestCase("CreateIntExpressionBody()")]
            [TestCase("CreateIntWithArg()")]
            [TestCase("CreateIntId()")]
            [TestCase("CreateIntSquare()")]
            [TestCase("Id<int>()")]
            public static void MethodReturningNotDisposable(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            // M();
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
}".AssertReplace("// M()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(expression);
                Assert.AreEqual(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Id(disposable)",                                   false)]
            [TestCase("Id<IDisposable>(null)",                            false)]
            [TestCase("this.Id<IDisposable>(null)",                       false)]
            [TestCase("this.Id<IDisposable>(this.disposable)",            false)]
            [TestCase("this.Id<IDisposable>(new Disposable())",           false)]
            [TestCase("this.Id<object>(new Disposable())",                false)]
            [TestCase("CreateDisposableStatementBody()",                  true)]
            [TestCase("this.CreateDisposableStatementBody()",             true)]
            [TestCase("CreateDisposableExpressionBody()",                 true)]
            [TestCase("CreateDisposableExpressionBodyReturnTypeObject()", true)]
            [TestCase("CreateDisposableInIf(true)",                       true)]
            [TestCase("CreateDisposableInElse(true)",                     true)]
            [TestCase("ReturningLocal()",                                 true)]
            public static void Call(string expression, bool expected)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        private readonly IDisposable disposable = new Disposable();

        internal C()
        {
            Id(disposable);
        }

        internal static IDisposable Id(IDisposable arg) => arg;

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
}".AssertReplace("Id(disposable)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(expression);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("StaticRecursiveStatementBody()",  false)]
            [TestCase("StaticRecursiveExpressionBody()", false)]
            [TestCase("CallingRecursive()",              false)]
            [TestCase("RecursiveTernary(true)",          true)]
            [TestCase("this.RecursiveExpressionBody()",  false)]
            [TestCase("this.RecursiveStatementBody()",   false)]
            public static void CallRecursiveMethod(string expression, bool expected)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            // M();
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
}".AssertReplace("// M()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(expression);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
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
            var value = disposable;
            value = WithOptionalParameter(value);
        }

        private static IDisposable WithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return WithOptionalParameter(value, new[] { value });
            }

            return value;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation("WithOptionalParameter(value)");
                Assert.AreEqual(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Task.Run(() => 1)",                         false)]
            [TestCase("Task.Run(() => new Disposable())",          false)]
            [TestCase("CreateStringAsync()",                       false)]
            [TestCase("await CreateStringAsync()",                 false)]
            [TestCase("await Task.Run(() => new string(' ', 1))",  false)]
            [TestCase("await Task.FromResult(new string(' ', 1))", false)]
            [TestCase("await Task.Run(() => new Disposable())",    true)]
            [TestCase("await Task.FromResult(new Disposable())",   true)]
            [TestCase("await CreateDisposableAsync()",             true)]
            public static void AsyncAwait(string expression, bool expected)
            {
                var code = @"
namespace N
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

    internal class C
    {
        internal async Task M()
        {
            var value = // M();
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
}".AssertReplace("// M()", expression);

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(expression)
                                      .Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void CompositeDisposableExtAddAndReturn()
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

        public C()
        {
            disposable.AddAndReturn(File.OpenRead(string.Empty));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation("disposable.AddAndReturn(File.OpenRead(string.Empty))");
                Assert.AreEqual(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.AsCustom()")]
            [TestCase("disposable.AsCustom() ?? other")]
            [TestCase("other ?? disposable.AsCustom()")]
            public static void AssumeYesForExtensionMethodReturningDifferentTypeThanThisParameter(string expression)
            {
                var binary = @"
namespace BinaryReference
{
    using System;

    public static class Extensions
    {
        public static ICustomDisposable AsCustom(this IDisposable disposable) => default(ICustomDisposable);
    }

    public interface ICustomDisposable : IDisposable
    {
    }
}";
                var code = @"
namespace N
{
    using System;
    using BinaryReference;

    class C
    {
        public C(IDisposable disposable, ICustomDisposable other)
        {
            _ = disposable.AsCustom();
        }
    }
}".AssertReplace("disposable.AsCustom()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var references = MetadataReferences.FromAttributes()
                                                   .Concat(new[] { MetadataReferences.CreateBinary(binary) });
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, references);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(true, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.Fluent()")]
            [TestCase("disposable.Fluent() ?? other")]
            [TestCase("other ?? disposable.Fluent()")]
            public static void AssumeNoForUnknownExtensionMethodReturningSameTypeAsThisParameter(string expression)
            {
                var binary = @"
namespace BinaryReference
{
    using System;

    public static class Extensions
    {
        public static IDisposable Fluent(this IDisposable disposable) => disposable;
    }
}";

                var code = @"
namespace N
{
    using System;
    using BinaryReference;

    class C
    {
        public C(IDisposable disposable, IDisposable other)
        {
            _ = disposable.Fluent();
        }
    }
}".AssertReplace("disposable.Fluent()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var references = MetadataReferences.FromAttributes()
                                                   .Concat(new[] { MetadataReferences.CreateBinary(binary) });
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, references);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("",                                                                                   "System.IO.File.OpenText(string.Empty)",                              true)]
            [TestCase("",                                                                                   "System.IO.File.OpenRead(string.Empty)",                              true)]
            [TestCase("",                                                                                   "System.IO.File.ReadAllLines(string.Empty)",                          false)]
            [TestCase("System.IO.FileInfo fileInfo",                                                        "fileInfo.Directory",                                                 false)]
            [TestCase("System.IO.FileInfo fileInfo",                                                        "fileInfo.OpenRead()",                                                true)]
            [TestCase("System.IO.FileInfo fileInfo",                                                        "fileInfo.ToString()",                                                false)]
            [TestCase("Microsoft.Win32.RegistryKey registryKey",                                            "registryKey.CreateSubKey(string.Empty)",                             true)]
            [TestCase("Microsoft.Win32.RegistryKey registryKey",                                            "registryKey.OpenSubKey(string.Empty)",                               true)]
            [TestCase("System.Collections.Generic.List<int> xs",                                            "xs.GetEnumerator()",                                                 true)]
            [TestCase("",                                                                                   "new System.Collections.Generic.List<IDisposable>().Find(x => true)", false)]
            [TestCase("",                                                                                   "ImmutableList<IDisposable>.Empty.Find(x => true)",                   false)]
            [TestCase("",                                                                                   "new Queue<IDisposable>().Peek()",                                    false)]
            [TestCase("",                                                                                   "ImmutableQueue<IDisposable>.Empty.Peek()",                           false)]
            [TestCase("",                                                                                   "new List<IDisposable>()[0]",                                         false)]
            [TestCase("",                                                                                   "Moq.Mock.Of<IDisposable>()",                                         false)]
            [TestCase("",                                                                                   "ImmutableList<IDisposable>.Empty[0]",                                false)]
            [TestCase("System.Windows.Controls.PasswordBox passwordBox",                                    "passwordBox.SecurePassword",                                         true)]
            [TestCase("System.Data.Entity.Infrastructure.SqlConnectionFactory factory",                     "factory.CreateConnection(string.Empty)",                             true)]
            [TestCase("System.Collections.Generic.List<int> xs",                                            "((System.Collections.IList)xs).GetEnumerator()",                     false)]
            [TestCase("System.Collections.Generic.List<IDisposable> xs",                                    "xs.First()",                                                         false)]
            [TestCase("System.Collections.Generic.Dictionary<int, IDisposable> map",                        "map[0]",                                                             false)]
            [TestCase("System.Collections.Generic.IReadOnlyDictionary<int, IDisposable> map",               "map[0]",                                                             false)]
            [TestCase("System.Runtime.CompilerServices.ConditionalWeakTable<IDisposable, IDisposable> map", "map.GetOrCreateValue(this.disposable)",                              false)]
            [TestCase("System.Resources.ResourceManager manager",                                           "manager.GetStream(null)",                                            false)]
            [TestCase("System.Resources.ResourceManager manager",                                           "manager.GetStream(null, null)",                                      false)]
            [TestCase("System.Resources.ResourceManager manager",                                           "manager.GetResourceSet(null, true, true)",                           false)]
            [TestCase("System.Net.Http.HttpResponseMessage message",                                        "message.EnsureSuccessStatusCode()",                                  false)]
            public static void ThirdParty(string parameter, string expression, bool expected)
            {
                var code = @"
namespace N
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    internal class C
    {
        internal C(int value)
        {
            _ = value;
        }
    }
}".AssertReplace("int value", parameter)
  .AssertReplace("value", expression);

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
                Assert.IsNotInstanceOf<IErrorTypeSymbol>(type);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Activator.CreateInstance<Disposable>()",                                                 true)]
            [TestCase("(Disposable)Activator.CreateInstance(typeof(Disposable))",                               true)]
            [TestCase("Activator.CreateInstance<System.Text.StringBuilder>()",                                  false)]
            [TestCase("Activator.CreateInstance(typeof(System.Text.StringBuilder))",                            false)]
            [TestCase("(System.Text.StringBuilder)Activator.CreateInstance(typeof(System.Text.StringBuilder))", false)]
            public static void Reflection(string expression, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.Reflection;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    class C
    {
        static void M(ConstructorInfo constructorInfo)
        {
            var value = Activator.CreateInstance<Disposable>();
        }
    }
}".AssertReplace("Activator.CreateInstance<Disposable>()", expression));
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(expression).Value;
                Assert.AreEqual(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
                Assert.IsNotInstanceOf<IErrorTypeSymbol>(type);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void Dump()
            {
                var set = new HashSet<string>();
                foreach (var method in typeof(Microsoft.Win32.RegistryKey).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
                {
                    if (!method.IsSpecialName &&
                        set.Add(method.Name))
                    {
                        Console.WriteLine($"                    IMethodSymbol {{ ContainingType: {{ MetadataName: \"{method.DeclaringType.Name}\" }}, MetadataName: \"{method.Name}\" }} => Result.{(typeof(IDisposable).IsAssignableFrom(method.ReturnType) ? "Yes" : "No")},");
                    }
                }
            }

            [Test]
            public static void DumpEnumerable()
            {
                foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.IsPublic && t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t))).OrderBy(x => x.Name))
                {
                    Console.WriteLine($"                    IMethodSymbol {{ ContainingType: {{ MetadataName: \"{type.Name}\" }} }} => false,");
                }
            }

            [TestCase("",                      "Factory.StaticDisposableField",     false)]
            [TestCase("",                      "Factory.StaticIDisposableProperty", false)]
            [TestCase("",                      "Factory.StaticCreateIDisposable()", true)]
            [TestCase("",                      "Factory.StaticCreateObject()",      false)]
            [TestCase("Factory factory",       "factory.IDisposableProperty",       false)]
            [TestCase("Factory factory",       "factory.CreateIDisposable()",       true)]
            [TestCase("Factory factory",       "factory.CreateObject()",            false)]
            [TestCase("Disposable disposable", "disposable.Id()",                   false)]
            [TestCase("Disposable disposable", "disposable.IdGeneric()",            false)]
            public static void Assumptions(string parameter, string expression, bool expected)
            {
                var binaryReference = BinaryReference.Compile(@"
namespace BinaryReferencedAssembly
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }

        public Disposable ReturnThis() => this;
    }

    public static class Ext
    {
        public static Disposable Id(this Disposable disposable) => disposable;

        public static T IdGeneric<T>(this T item) => item;
    }

    public class Factory
    {
        public static readonly IDisposable StaticDisposableField = new Disposable();

        public static IDisposable StaticIDisposableProperty => StaticDisposableField;

        public IDisposable IDisposableProperty => StaticDisposableField;

        public static IDisposable StaticCreateIDisposable() => new Disposable();

        public static object StaticCreateObject() => new object();

        public IDisposable CreateIDisposable() => new Disposable();

        public object CreateObject() => new object();
    }
}");

                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using BinaryReferencedAssembly;

    class C
    {
        static void M(Factory factory)
        {
            factory.CreateIDisposable();
        }
    }
}".AssertReplace("Factory factory", parameter)
  .AssertReplace("factory.CreateIDisposable()", expression));

                var compilation = CSharpCompilation.Create(
                    "test",
                    new[] { syntaxTree },
                    MetadataReferences.FromAttributes().Add(binaryReference),
                    CodeFactory.DllCompilationOptions.WithMetadataImportOptions(MetadataImportOptions.Public));
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
                Assert.IsNotInstanceOf<IErrorTypeSymbol>(type);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Interlocked.Exchange(ref _disposable, new MemoryStream())")]
            [TestCase("Interlocked.Exchange(ref _disposable, null)")]
            public static void InterlockedExchange(string expression)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.IO;
    using System.Threading;

    sealed class C : IDisposable
    {
        private IDisposable _disposable = new MemoryStream();

        public void Update()
        {
            var oldValue = Interlocked.Exchange(ref _disposable, new MemoryStream());
        }
    }
}".AssertReplace("Interlocked.Exchange(ref _disposable, new MemoryStream())", expression));

                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(expression);
                Assert.AreEqual(true, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
