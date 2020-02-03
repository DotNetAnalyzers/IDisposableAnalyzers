namespace IDisposableAnalyzers.NetCoreTests.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [Ignore("Not sure how we want the code gen.")]
    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
        private static readonly CodeFixProvider Fix = new DisposeMemberFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP002DisposeMember);

        [Test]
        public static void FieldIAsyncDisposable()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        ↓private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public ValueTask DisposeAsync()
        {
        }
    }
}";

            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, code);
        }

        [Test]
        public static void FieldOfTypeObjectIAsyncDisposable()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        ↓private readonly object disposable = File.OpenRead(string.Empty);

        public ValueTask DisposeAsync()
        {
        }
    }
}";

            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private readonly object disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)this.disposable).DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, code);
        }

        [Test]
        public static void FieldIAsyncDisposableAndIDisposable1()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        ↓private readonly Stream disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        public ValueTask DisposeAsync()
        {
        }
    }
}";

            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, code);
        }

        [Test]
        public static void FieldIAsyncDisposableAndIDisposable2()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        ↓private readonly Stream disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
        }
    }
}";

            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        private readonly Stream disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, code);
        }
    }
}
