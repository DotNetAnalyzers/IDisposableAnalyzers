namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddUsingDeclaration
        {
            private static readonly DiagnosticAnalyzer Analyzer = new LocalDeclarationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);
            private static readonly CodeFixProvider Fix = new AddUsingFix();

            private const string Disposable = @"
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

            [Test]
            public static void LocalToUsingDeclaration()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using var stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
            }

            [Test]
            public static void Local()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [Test]
            public static void LocalWithTriviaToUsingDeclaration()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            // Some comment
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            // Some comment
            using var stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
            }

            [Test]
            public static void LocalWithTrivia()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            // Some comment
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            // Some comment
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [Test]
            public static void LocalOneStatementAfterToUsingDeclaration()
            {
                var before = @"
#pragma warning disable CS0219
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";

                var after = @"
#pragma warning disable CS0219
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using var stream = File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
            }

            [Test]
            public static void LocalOneStatementAfter()
            {
                var before = @"
#pragma warning disable CS0219
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";

                var after = @"
#pragma warning disable CS0219
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var i = 1;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [Test]
            public static void LocalManyStatements()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public int M()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var a = 1;
            var b = 1;
            if (a == b)
            {
                var c = 2;
                return a + b + c;
            }

            return a + b;
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public int M()
        {
            using var stream = File.OpenRead(string.Empty);
            var a = 1;
            var b = 1;
            if (a == b)
            {
                var c = 2;
                return a + b + c;
            }

            return a + b;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
            }

            [Test]
            public static void LocalInLambdaToUsingDeclaration()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                ↓var stream = File.OpenRead(string.Empty);
            };
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                using var stream = File.OpenRead(string.Empty);
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
            }

            [Test]
            public static void LocalInLambda()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                ↓var stream = File.OpenRead(string.Empty);
            };
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                using (var stream = File.OpenRead(string.Empty))
                {
                }
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [Test]
            public static void LocalManyStatementsToUsingDeclaration()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public int M()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var a = 1;
            var b = 1;
            if (a == b)
            {
                var c = 2;
                return a + b + c;
            }

            return a + b;
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public int M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var a = 1;
                var b = 1;
                if (a == b)
                {
                    var c = 2;
                    return a + b + c;
                }

                return a + b;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [Test]
            public static void LocalInSwitchCase()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    class C
    {
        public C(StringComparison comparison)
        {
            switch (comparison)
            {
                case StringComparison.CurrentCulture:
                    break;
                case StringComparison.CurrentCultureIgnoreCase:
                    break;
                case StringComparison.InvariantCulture:
                    break;
                case StringComparison.InvariantCultureIgnoreCase:
                    break;
                case StringComparison.Ordinal:
                    break;
                case StringComparison.OrdinalIgnoreCase:
                    ↓var stream = File.OpenRead(string.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null);
            }
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    class C
    {
        public C(StringComparison comparison)
        {
            switch (comparison)
            {
                case StringComparison.CurrentCulture:
                    break;
                case StringComparison.CurrentCultureIgnoreCase:
                    break;
                case StringComparison.InvariantCulture:
                    break;
                case StringComparison.InvariantCultureIgnoreCase:
                    break;
                case StringComparison.Ordinal:
                    break;
                case StringComparison.OrdinalIgnoreCase:
                    using (var stream = File.OpenRead(string.Empty))
                    {
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [Test]
            public static void LocalFactoryMethod()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            ↓var stream = Create();

            IDisposable Create()
            {
                return File.OpenRead(string.Empty);
            }
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            using (var stream = Create())
            {
            }

            IDisposable Create()
            {
                return File.OpenRead(string.Empty);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Add using to end of block.");
            }

            [TestCase("System.Activator.CreateInstance<Disposable>()")]
            [TestCase("(Disposable)System.Activator.CreateInstance(typeof(Disposable))!")]
            [TestCase("(Disposable?)System.Activator.CreateInstance(typeof(Disposable))")]
            [TestCase("(Disposable)constructorInfo.Invoke(null)")]
            public static void Reflection(string expression)
            {
                var before = @"
namespace N
{
    using System.Reflection;

    public class C
    {
        public static void M(ConstructorInfo constructorInfo)
        {
            ↓var disposable = Activator.CreateInstance<Disposable>();
        }
    }
}".AssertReplace("Activator.CreateInstance<Disposable>()", expression);

                var after = @"
namespace N
{
    using System.Reflection;

    public class C
    {
        public static void M(ConstructorInfo constructorInfo)
        {
            using var disposable = Activator.CreateInstance<Disposable>();
        }
    }
}".AssertReplace("Activator.CreateInstance<Disposable>()", expression);
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after, fixTitle: "using");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after, fixTitle: "using");
            }

            [Test]
            public static void CreateRebind()
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

                var before = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static void M()
        {
            ↓var kernel = Create().Rebind<IDisposable, Disposable>();
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            return container;
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static void M()
        {
            using var kernel = Create().Rebind<IDisposable, Disposable>();
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            return container;
        }
    }
}";

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { disposable, before }, after, fixTitle: "using");
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { disposable, before }, after, fixTitle: "using");
            }
        }
    }
}
