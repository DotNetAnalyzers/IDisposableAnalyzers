namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    [Test]
    public static void FileOpenRead()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void NewStreamReader()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var reader = new StreamReader(File.OpenRead(string.Empty)))
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("await Task.FromResult(new Disposable())")]
    [TestCase("await Task.FromResult(new Disposable()).ConfigureAwait(false)")]
    [TestCase("await Task.Run(() => new Disposable())")]
    [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)")]
    [TestCase("Task.Run(() => new Disposable()).Result")]
    [TestCase("Task.Run(() => new Disposable()).GetAwaiter().GetResult()")]
    public static void AwaitSimple(string expression)
    {
        var code = @"
namespace N
{
    using System.Threading.Tasks;

    class C
    {
        public async Task M()
        {
            using (await Task.FromResult(new Disposable()))
            {
            }
            
            await Task.Delay(10);
        }
    }
}".AssertReplace("await Task.FromResult(new Disposable())", expression);
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [TestCase("await Task.FromResult(new Disposable())")]
    [TestCase("await Task.FromResult(new Disposable()).ConfigureAwait(false)")]
    [TestCase("Task.FromResult(new Disposable()).GetAwaiter().GetResult()")]
    [TestCase("await Task.Run(() => new Disposable())")]
    [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)")]
    [TestCase("Task.Run(() => new Disposable()).Result")]
    [TestCase("Task.Run(() => new Disposable()).GetAwaiter().GetResult()")]
    public static void AwaitSimpleUsingDeclaration(string expression)
    {
        var code = @"
namespace N
{
    using System.Threading.Tasks;

    class C
    {
        public async Task M()
        {
            using var disposable = await Task.FromResult(new Disposable());
            await Task.Delay(10);
        }
    }
}".AssertReplace("await Task.FromResult(new Disposable())", expression);
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void AwaitWeirdCase()
    {
        var code = @"
#nullable disable
namespace N
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class C
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UsingChainedReturningThis()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable M() => this;

        public void Dispose()
        {
        }
    }
}";

        var code = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            using var disposable = new Disposable().M();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, code);
    }

    [Test]
    public static void UsingChainedReturningThisExpressionStatement()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable M() => this;

        public void Dispose()
        {
        }
    }
}";

        var code = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            using var disposable = new Disposable();
            disposable.M();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, code);
    }

    [Test]
    public static void UsingChainedExtension()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var disposableExt = @"
namespace N
{
    public static class DisposableExt
    {
        public static Disposable M<Disposable>(this Disposable disposable) => disposable;
    }
}";

        var code = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            using var disposable = new Disposable().M();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, disposableExt, code);
    }

    [Test]
    public static void UsingChainedExtensionExpressionStatement()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var disposableExt = @"
namespace N
{
    public static class DisposableExt
    {
        public static Disposable M<Disposable>(this Disposable disposable) => disposable;
    }
}";

        var code = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            using var disposable = new Disposable();
            disposable.M();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, disposableExt, code);
    }

    [Test]
    public static void NewKernelBindStatement()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static void NewKernelBind()
        {
            using var kernel = new Kernel();
            kernel.Bind<IDisposable, Disposable>();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, code);
    }
}
