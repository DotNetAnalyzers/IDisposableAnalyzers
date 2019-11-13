namespace IDisposableAnalyzers.Test.IDISP016DoNotUseDisposedInstanceTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        public static class DisposeCall
        {
            private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP016DoNotUseDisposedInstance);

            [Test]
            public static void CreateTouchDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream.Dispose();
            var b = stream.ReadByte();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void DisposingTwice()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream.Dispose();
            stream.Dispose();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void DisposingTwiceInUsing()
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
                ↓stream.Dispose();
                stream.Dispose();
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void AssignedViaOut()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            Stream stream;
            Create(out stream);
            var b = stream.ReadByte();
            ↓stream.Dispose();
            b = stream.ReadByte();
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void AssignedViaOutVar()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            Create(out var stream);
            var b = stream.ReadByte();
            ↓stream.Dispose();
            b = stream.ReadByte();
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void ReassignAfterDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            b = stream.ReadByte();
            ↓stream.Dispose();
            b = stream.ReadByte();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void ReassignViaOutVarAfterDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            Create(out var stream);
            var b = stream.ReadByte();
            stream.Dispose();
            Create(out stream);
            b = stream.ReadByte();
            ↓stream.Dispose();
            b = stream.ReadByte();
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void ReassignViaOutAfterDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            Stream stream;
            Create(out stream);
            var b = stream.ReadByte();
            stream.Dispose();
            Create(out stream);
            b = stream.ReadByte();
            ↓stream.Dispose();
            b = stream.ReadByte();
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }
        }
    }
}
