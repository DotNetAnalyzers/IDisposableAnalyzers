namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFixAll : CodeFixVerifier<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>
    {
        [Test]
        public async Task NotDisposingVariable()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream = File.OpenRead(string.Empty);
        stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream = File.OpenRead(string.Empty);
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingVariables()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream1 = File.OpenRead(string.Empty);
        var stream2 = File.OpenRead(string.Empty);
        stream1 = File.OpenRead(string.Empty);
        stream2 = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream1 = File.OpenRead(string.Empty);
        var stream2 = File.OpenRead(string.Empty);
        stream1?.Dispose();
        stream1 = File.OpenRead(string.Empty);
        stream2?.Dispose();
        stream2 = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }
    }
}