namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class RefAndOut
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();

            [Test]
            public static void LocalViaObjectCreationThenOutParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public void M()
        {
            var disposable = new Disposable();
            TryM(↓out disposable);
        }

        public static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
            return true;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public void M()
        {
            var disposable = new Disposable();
            disposable?.Dispose();
            TryM(out disposable);
        }

        public static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
            return true;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            }

            [Test]
            public static void LocalInvocationThenOutParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void LocalViaOutTwice()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M()
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

    public class C
    {
        public void M()
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldViaOutInPublicMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public void Update()
        {
            TryGetStream(↓out this.stream);
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

    public class C
    {
        private Stream stream;

        public void Update()
        {
            this.stream?.Dispose();
            TryGetStream(out this.stream);
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldViaOutInPublicMethodNoThis()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
    {
        private Stream stream;

        public void Update()
        {
            stream?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldOfTypeObjectViaOutParameterInPublicMethodNoThis()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private object stream;

        public void Update()
        {
            TryGetStream(↓out stream);
        }

        public bool TryGetStream(out object outValue)
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

    public class C
    {
        private object stream;

        public void Update()
        {
            (stream as IDisposable)?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out object outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldPrivateMethodRef()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public C()
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

    public sealed class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public C()
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldPrivateMethodRefTwice()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream;

        public C()
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

    public sealed class C : IDisposable
    {
        private readonly Stream stream;

        public C()
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldPrivateMethodRefTwiceDifferentMethods()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream;

        public C()
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

    public sealed class C : IDisposable
    {
        private readonly Stream stream;

        public C()
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldPublicMethodRef()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void M1()
        {
            this.M2(↓ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        public void M2(ref Stream stream)
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

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void M1()
        {
            this.stream?.Dispose();
            this.M2(ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        public void M2(ref Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public static void FieldPublicMethodRefExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void M1() => this.M2(↓ref this.stream);

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        public void M2(ref Stream stream)
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

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void M1()
        {
            this.stream?.Dispose();
            this.M2(ref this.stream);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }

        public void M2(ref Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
