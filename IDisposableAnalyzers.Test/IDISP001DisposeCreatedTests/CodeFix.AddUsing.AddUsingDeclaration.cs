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

            [Test]
            public static void Local()
            {
                var before = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalWithTrivia()
            {
                var before = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalOneStatementAfter()
            {
                var before = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalManyStatements()
            {
                var before = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var a = 1;
            var b = 1;
            if (a == b)
            {
                var c = 2;
            }
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var a = 1;
                var b = 1;
                if (a == b)
                {
                    var c = 2;
                }
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalInLambda()
            {
                var before = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalInSwitchCase()
            {
                var before = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void LocalFactoryMethod()
            {
                var before = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
