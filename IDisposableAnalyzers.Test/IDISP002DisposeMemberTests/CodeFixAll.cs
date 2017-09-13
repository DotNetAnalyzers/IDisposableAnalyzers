namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFixAll : CodeFixVerifier<IDISP002DisposeMember, DisposeMemberCodeFixProvider>
    {
        [Test]
        public async Task NotDisposingFieldAssignedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo()
    {
        this.stream = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
    }
}";

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo()
    {
        this.stream = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.stream?.Dispose();
    }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldsAssignedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream1;
    private readonly Stream stream2;

    public Foo()
    {
        this.stream1 = File.OpenRead(string.Empty);
        this.stream2 = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
    }
}";

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream1;
    private readonly Stream stream2;

    public Foo()
    {
        this.stream1 = File.OpenRead(string.Empty);
        this.stream2 = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.stream1?.Dispose();
        this.stream2?.Dispose();
    }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}