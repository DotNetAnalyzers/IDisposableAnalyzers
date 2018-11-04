namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class RefAndOut
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();

            [Test]
            public void AssigningFieldViaOutParameterInPublicMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Update()
        {
            TryGetStream(↓out stream);
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Update()
        {
            this.stream?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssigningVariableViaOutParameterAfter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Update()
        {
            Stream stream = File.OpenRead(string.Empty);
            TryGetStream(↓out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Update()
        {
            Stream stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssigningVariableViaOutParameterTwice()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream;
            TryGetStream(out stream);
            TryGetStream(↓out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream;
            TryGetStream(out stream);
            stream?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CallPrivateMethodRefParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.Assign(↓ref this.stream);
        }

        public void Dispose()
        {
            stream?.Dispose();
        }

        private void Assign(ref Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.stream?.Dispose();
            this.Assign(ref this.stream);
        }

        public void Dispose()
        {
            stream?.Dispose();
        }

        private void Assign(ref Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CallPrivateMethodRefParameterTwice()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            this.Assign(ref this.stream);
            this.Assign(↓ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        private void Assign(ref Stream arg)
        {
            arg = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            this.Assign(ref this.stream);
            this.stream?.Dispose();
            this.Assign(ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        private void Assign(ref Stream arg)
        {
            arg = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CallPrivateMethodRefParameterTwiceDifferentMethods()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            this.Assign1(ref this.stream);
            this.Assign2(↓ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        private void Assign1(ref Stream arg)
        {
            arg = File.OpenRead(string.Empty);
        }

        private void Assign2(ref Stream arg)
        {
            arg = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            this.Assign1(ref this.stream);
            this.stream?.Dispose();
            this.Assign2(ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        private void Assign1(ref Stream arg)
        {
            arg = File.OpenRead(string.Empty);
        }

        private void Assign2(ref Stream arg)
        {
            arg = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void CallPublicMethodRefParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        private Foo()
        {
            this.Assign(↓ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        public void Assign(ref Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        private Foo()
        {
            this.stream?.Dispose();
            this.Assign(ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        public void Assign(ref Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
