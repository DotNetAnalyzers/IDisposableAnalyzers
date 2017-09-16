namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFixAddToCompositeDisposable
    {
        [Test]
        public void AddIgnoredReturnValueToCreatedCompositeDisposableCtor()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToCreatedCompositeDisposableCtorUsingsAndFields()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToCreatedCompositeDisposableCtorUsingsAndFieldsUnderscoreNames()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToCreatedCompositeDisposableInitializer()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToExistingCompositeDisposableInitializerOneLine()
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
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            ↓File.OpenRead(string.Empty);
        }
    }
}";

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
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty)
            };
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToExistingCompositeDisposableInitializer()
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
            this.disposable = new CompositeDisposable()
            {
                File.OpenRead(string.Empty)
            };
            ↓File.OpenRead(string.Empty);
        }
    }
}";

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
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty)
            };
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Explicit("Fix later.")]
        [Test]
        public void AddIgnoredReturnValueToExistingCompositeDisposableInitializerWithComment()
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

            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToExistingCompositeDisposableCtor()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToExistingCompositeDisposableCtorUnderscore()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddIgnoredReturnValueToCompositeDisposableInitializer()
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
            AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>(testCode, fixedCode);
        }
    }
}