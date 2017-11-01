namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class AddUsing
        {
            [Test]
            public void AddUsingForIgnoredReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (File.OpenRead(string.Empty))
            {
                var i = 1;
            }
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void AddUsingForIgnoredReturnEmpty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
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

    public sealed class Foo
    {
        public void Meh()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void AddUsingForIgnoredReturnManyStatements()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓File.OpenRead(string.Empty);
            var a = 1;
            var b = 2;
            if (a == b)
            {
                var c = 3;
            }

            var d = 4;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (File.OpenRead(string.Empty))
            {
                var a = 1;
                var b = 2;
                if (a == b)
                {
                    var c = 3;
                }

                var d = 4;
            }
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP004DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>(testCode, fixedCode);
            }
        }
    }
}