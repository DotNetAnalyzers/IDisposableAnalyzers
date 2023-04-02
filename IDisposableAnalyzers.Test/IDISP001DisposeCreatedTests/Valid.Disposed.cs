namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

// ReSharper disable once UnusedTypeParameter
public static partial class Valid<T>
{
    [TestCase("stream.Dispose()")]
    [TestCase("stream?.Dispose()")]
    [TestCase("((IDisposable)stream)?.Dispose()")]
    [TestCase("(stream as IDisposable)?.Dispose()")]
    public static void DisposedLocal(string expression)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream = File.OpenRead(String.Empty);
            stream.Dispose();
        }
    }
}".AssertReplace("stream.Dispose()", expression);

        RoslynAssert.Valid(Analyzer, Descriptor, code);
    }

    [TestCase("using (var stream = File.OpenRead(string.Empty))")]
    [TestCase("using (File.OpenRead(string.Empty))")]
    public static void UsedLocal(string expression)
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}".AssertReplace("using (var stream = File.OpenRead(string.Empty))", expression);

        RoslynAssert.Valid(Analyzer, Descriptor, code);
    }

    [TestCase("new DefaultFalse(stream)")]
    [TestCase("new DefaultFalse(stream, false)")]
    [TestCase("new DefaultTrue(stream, false)")]
    public static void LeaveOpenDispose(string expression)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            var stream = File.OpenRead(fileName);
            using var reader = new DefaultFalse(stream);
        }

        private sealed class DefaultTrue : IDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultTrue(Stream stream, bool leaveOpen = true)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public void Dispose()
            {
                if (!this.leaveOpen)
                {
                    this.stream.Dispose();
                }
            }
        }

        private sealed class DefaultFalse : IDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultFalse(Stream stream, bool leaveOpen = false)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public void Dispose()
            {
                if (!this.leaveOpen)
                {
                    this.stream.Dispose();
                }
            }
        }
    }
}".AssertReplace("new DefaultFalse(stream)", expression);

        RoslynAssert.Valid(Analyzer, Descriptor, code);
    }

    [TestCase("new DefaultFalse(stream)")]
    [TestCase("new DefaultFalse(stream, false)")]
    [TestCase("new DefaultTrue(stream, false)")]
    public static void LeaveOpenWhenDisposeAsync(string expression)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public static async Task M(string fileName)
        {
            var stream = File.OpenRead(fileName);
            await using var reader = new DefaultFalse(stream);
        }

        private sealed class DefaultTrue : IAsyncDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultTrue(Stream stream, bool leaveOpen = true)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public ValueTask DisposeAsync()
            {
                if (!this.leaveOpen)
                {
                    return this.stream.DisposeAsync();
                }

                return ValueTask.CompletedTask;
            }
        }

        private sealed class DefaultFalse : IAsyncDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultFalse(Stream stream, bool leaveOpen = false)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public ValueTask DisposeAsync()
            {
                if (!this.leaveOpen)
                {
                    return this.stream.DisposeAsync();
                }

                return ValueTask.CompletedTask;
            }
        }
    }
}
".AssertReplace("new DefaultFalse(stream)", expression);

        RoslynAssert.Valid(Analyzer, Descriptor, code);
    }
}
