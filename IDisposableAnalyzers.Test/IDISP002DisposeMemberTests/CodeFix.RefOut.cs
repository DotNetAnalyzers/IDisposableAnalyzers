namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<IDISP002DisposeMember, DisposeMemberCodeFixProvider>
    {
        internal class RefAndOut : NestedCodeFixVerifier<CodeFix>
        {
            [Test]
            public async Task AssigningFieldViaOutParameterInCtor()
            {
                var testCode = @"
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
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose member.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
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
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningFieldViaRefParameterInCtor()
            {
                var testCode = @"
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
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose member.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
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
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }
        }
    }
}