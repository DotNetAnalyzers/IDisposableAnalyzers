namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddToCompositeDisposable
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP004DoNotIgnoreCreated);
            private static readonly CodeFixProvider Fix = new AddToCompositeDisposableFix();

            [Test]
            public static void CreateNewCompositeDisposable()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        internal C()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void CreateNewCompositeDisposableWithTrivia()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        internal C()
        {
            ↓File.OpenRead(string.Empty); // trivia
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty), // trivia
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void CreateNewCompositeDisposableWhenUsingsAndFields()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;

        private int value4;

        internal C()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;
        private readonly System.Reactive.Disposables.CompositeDisposable disposable;
        private int value4;

        internal C()
        {
            this.disposable = new System.Reactive.Disposables.CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void CreateNewCompositeDisposableWhenUsingsAndFieldsUnderscoreNames()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;

        private int _value4;

        internal C()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C
    {
        const int value1 = 2;

        private static readonly int value2;

        private static int value3;
        private readonly System.Reactive.Disposables.CompositeDisposable _disposable;
        private int _value4;

        internal C()
        {
            _disposable = new System.Reactive.Disposables.CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddToExistingCompositeDisposableInitializer()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable();
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddToExistingCompositeDisposableInitializerWithCtorArg()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable(1);
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable(1) { File.OpenRead(string.Empty) };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddToExistingCompositeDisposableInitializerWithTrivia()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable(); // trivia1
            ↓File.OpenRead(string.Empty); // trivia2
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty), // trivia2
            }; // trivia1
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddToExistingCompositeDisposableWithInitializerOneLine()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty),
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddWithTriviaToExistingCompositeDisposableWithInitializerOneLine()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            ↓File.OpenRead(string.Empty); // trivia
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty), // trivia
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIgnoredReturnValueToExistingCompositeDisposableInitializer()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable()
            {
                File.OpenRead(string.Empty)
            };
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty),
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIgnoredReturnValueToExistingCompositeDisposableInitializerWithCtorArg()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable(1)
            {
                File.OpenRead(string.Empty)
            };
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable(1)
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty),
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIgnoredReturnValueToExistingCompositeDisposableInitializerWithComment()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable()
            {
                File.OpenRead(string.Empty) // comment
            };
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty), // comment
                File.OpenRead(string.Empty),
            };
        }
    }
}";

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIgnoredReturnValueToExistingCompositeDisposableCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal C()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal C()
        {
            this.disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIgnoredReturnValueToExistingCompositeDisposableCtorUnderscore()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        internal C()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        internal C()
        {
            _disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIgnoredReturnValueToCompositeDisposableInitializer()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal C()
        {
            this.disposable = new CompositeDisposable();
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddToExistingCompositeDisposableWithInitializerOneLineWithStatementsBetween()
            {
                var before = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            var i = 1;
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class C
    {
        private readonly CompositeDisposable disposable;

        internal C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
            var i = 1;
            this.disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
