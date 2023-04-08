namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    public static class Ignore
    {
        [Test]
        public static void NUnitAssertThrowsAsync()
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;
                    using System.Threading.Tasks;
                    using NUnit.Framework;

                    public class C
                    {
                        public void M()
                        {
                            using (var stream = File.OpenRead(string.Empty))
                            {
                                Assert.ThrowsAsync<Exception>(() => Task.Run(() => throw new Exception()));
                            }
                        }
                    }
                }
                """;
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
