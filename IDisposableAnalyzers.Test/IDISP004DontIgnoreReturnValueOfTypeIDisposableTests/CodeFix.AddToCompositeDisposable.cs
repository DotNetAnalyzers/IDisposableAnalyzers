namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class AddToCompositeDisposable
        {
            private static readonly IDISP004DontIgnoreReturnValueOfTypeIDisposable Analyzer = new IDISP004DontIgnoreReturnValueOfTypeIDisposable();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP004");
            private static readonly AddToCompositeDisposableCodeFixProvider Fix = new AddToCompositeDisposableCodeFixProvider();

            [Test]
            public void CreateNewCompositeDisposable()
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
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CreateNewCompositeDisposableWithTrivia()
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
            ↓File.OpenRead(string.Empty); // trivia
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
                File.OpenRead(string.Empty), // trivia
            };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CreateNewCompositeDisposableWhenUsingsAndFields()
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
            this.disposable = new System.Reactive.Disposables.CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CreateNewCompositeDisposableWhenUsingsAndFieldsUnderscoreNames()
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
            _disposable = new System.Reactive.Disposables.CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddToExistingCompositeDisposableInitializer()
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
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddToExistingCompositeDisposableInitializerWithCtorArg()
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
            this.disposable = new CompositeDisposable(1);
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
            this.disposable = new CompositeDisposable(1) { File.OpenRead(string.Empty) };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddToExistingCompositeDisposableInitializerWithTrivia()
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
            this.disposable = new CompositeDisposable(); // trivia1
            ↓File.OpenRead(string.Empty); // trivia2
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
                File.OpenRead(string.Empty), // trivia2
            }; // trivia1
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddToExistingCompositeDisposableWithInitializerOneLine()
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
                File.OpenRead(string.Empty),
            };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddWithTriviaToExistingCompositeDisposableWithInitializerOneLine()
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
            ↓File.OpenRead(string.Empty); // trivia
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
                File.OpenRead(string.Empty), // trivia
            };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
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
                File.OpenRead(string.Empty),
            };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddIgnoredReturnValueToExistingCompositeDisposableInitializerWithCtorArg()
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
            this.disposable = new CompositeDisposable(1)
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
            this.disposable = new CompositeDisposable(1)
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty),
            };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

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
                File.OpenRead(string.Empty),
            };
        }
    }
}";

                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
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
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddToExistingCompositeDisposableWithInitializerOneLineWithStatementsBetween()
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
            var i = 1;
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
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            var i = 1;
            this.disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
