namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class RefAndOut
        {
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void AssigningVariableViaOutParameterBefore()
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
            if (this.TryGetStream(out stream))
            {
                ↓stream = File.OpenRead(string.Empty);
            }
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
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
            if (this.TryGetStream(out stream))
            {
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
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
                AnalyzerAssert.CodeFix<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>(testCode, fixedCode);
            }
        }
    }
}