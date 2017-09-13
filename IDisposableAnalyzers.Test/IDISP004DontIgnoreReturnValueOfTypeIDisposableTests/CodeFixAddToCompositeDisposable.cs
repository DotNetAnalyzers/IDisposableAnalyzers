namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFixAddToCompositeDisposable : CodeFixVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>
    {
        [Test]
        public async Task AddIgnoredReturnValueToCreatedCompositeDisposableCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        internal Foo()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable() { File.OpenRead(string.Empty) };
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToCreatedCompositeDisposableCtorUsingsAndFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;

        private int value4;

        internal Foo()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;
        private readonly System.Reactive.Disposables.CompositeDisposable disposable;
        private int value4;

        internal Foo()
        {
            this.disposable = new System.Reactive.Disposables.CompositeDisposable() { File.OpenRead(string.Empty) };
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToCreatedCompositeDisposableCtorUsingsAndFieldsUnderscoreNames()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;

        private int _value4;

        internal Foo()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;
        private readonly System.Reactive.Disposables.CompositeDisposable _disposable;
        private int _value4;

        internal Foo()
        {
            _disposable = new System.Reactive.Disposables.CompositeDisposable() { File.OpenRead(string.Empty) };
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToCreatedCompositeDisposableInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable();
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable() { File.OpenRead(string.Empty) };
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToExistingCompositeDisposableInitializerOneLine()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty)
            };
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToExistingCompositeDisposableInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable()
            {
                File.OpenRead(string.Empty)
            };
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty)
            };
        }
    }
}";

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Explicit("Fix later.")]
        [Test]
        public async Task AddIgnoredReturnValueToExistingCompositeDisposableInitializerWithComment()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable()
            {
                File.OpenRead(string.Empty) // comment
            };
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable;

        internal Foo()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty), // comment
                File.OpenRead(string.Empty)
            };
        }
    }
}";

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToExistingCompositeDisposableCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal Foo()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal Foo()
        {
            this.disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToExistingCompositeDisposableCtorUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        internal Foo()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        internal Foo()
        {
            _disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AddIgnoredReturnValueToCompositeDisposableInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal Foo()
        {
            this.disposable = new CompositeDisposable();
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal Foo()
        {
            this.disposable = new CompositeDisposable() { File.OpenRead(string.Empty) };
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}