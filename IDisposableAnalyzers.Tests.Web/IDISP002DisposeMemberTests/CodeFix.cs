namespace IDisposableAnalyzers.Tests.Web.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
        private static readonly CodeFixProvider Fix = new DisposeMemberFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP002DisposeMember);

        [Test]
        public static void FieldIAsyncDisposable()
        {
            var before = @"
#nullable enable
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

            var after = @"
#nullable enable
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldOfTypeObjectIAsyncDisposable()
        {
            var before = @"
#nullable enable
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

            var after = @"
#nullable enable
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldIAsyncDisposableAndIDisposable1()
        {
            var before = @"
#nullable enable
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

            var after = @"
#nullable enable
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
            await this.disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldIAsyncDisposableAndIDisposable2()
        {
            var before = @"
#nullable enable
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

            var after = @"
#nullable enable
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void NullableFieldIAsyncDisposable()
        {
            var code = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        ↓private readonly IAsyncDisposable? disposable = File.OpenRead(string.Empty);

        public ValueTask DisposeAsync()
        {
            return default(ValueTask);
        }
    }
}";

            RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, code);
        }

        [Test]
        public static void FieldConvertibleToIDisposable()
        {
            var before = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;

    sealed class C : IDisposable
    {
        ↓private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;

    sealed class C : IDisposable
    {
        private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.disposable).Dispose();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldConvertibleToIDisposableAndIAsyncDisposable1()
        {
            var before = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        ↓private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.disposable).Dispose();
        }

        public ValueTask DisposeAsync()
        {
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.disposable).Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)this.disposable).DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldConvertibleToIDisposableAndIAsyncDisposable2()
        {
            var before = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        ↓private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)this.disposable).DisposeAsync().ConfigureAwait(false);
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.disposable).Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)this.disposable).DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldConvertibleToIDisposableAndIAsyncDisposable3()
        {
            var before = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        ↓private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable, IAsyncDisposable
    {
        private readonly object disposable = File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.disposable).Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)this.disposable).DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
