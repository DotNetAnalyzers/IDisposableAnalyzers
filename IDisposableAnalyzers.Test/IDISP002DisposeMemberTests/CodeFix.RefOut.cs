namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class RefAndOut
        {
            [Test]
            public void AssigningFieldViaOutParameterInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo()
        {
            if(TryGetStream(out this.stream))
            {
            }
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
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
            if(TryGetStream(out this.stream))
            {
            }
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public async Task AssigningFieldViaRefParameterInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo()
        {
            if(TryGetStream(ref this.stream))
            {
            }
        }

        public bool TryGetStream(ref Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
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
            if(TryGetStream(ref this.stream))
            {
            }
        }

        public bool TryGetStream(ref Stream outValue)
        {
            outValue = File.OpenRead(string.Empty);
            return true;
        }


        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }
        }
    }
}