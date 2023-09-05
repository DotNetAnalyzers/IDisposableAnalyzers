﻿// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    private static readonly ReturnValueAnalyzer Analyzer = new();

    [Test]
    public static void AwaitWebClientDownloadStringTaskAsyncInUsing()
    {
        var code = """
            #pragma warning disable SYSLIB0014
            namespace N
            {
                using System.Net;
                using System.Threading.Tasks;

                public class C
                {
                    public async Task<string> M()
                    {
                        using (var client = new WebClient())
                        {
                            return await client.DownloadStringTaskAsync(string.Empty);
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void AwaitWebClientDownloadStringTaskAsyncInUsingDeclaration()
    {
        var code = """
            #pragma warning disable SYSLIB0014
            namespace N
            {
                using System.Net;
                using System.Threading.Tasks;

                public class C
                {
                    public async Task<string> M()
                    {
                        using var client = new WebClient();
                        return await client.DownloadStringTaskAsync(string.Empty);
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UsingAwaited()
    {
        var code = """
            namespace N
            {
                using System.IO;
                using System.Threading.Tasks;

                public class C
                {
                    public static async Task<string?> MAsync()
                    {
                        using (var stream = await ReadAsync(string.Empty).ConfigureAwait(false))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadLine();
                            }
                        }
                    }

                    private static async Task<Stream> ReadAsync(string fileName)
                    {
                        var stream = new MemoryStream();
                        using (var fileStream = File.OpenRead(fileName))
                        {
                            await fileStream.CopyToAsync(stream)
                                            .ConfigureAwait(false);
                        }

                        stream.Position = 0;
                        return stream;
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void TaskFromResult()
    {
        var code = """
            namespace N
            {
                using System.IO;
                using System.Threading.Tasks;

                public class C
                {
                    public Task<int> M()
                    {
                        using (var stream = File.OpenRead(string.Empty))
                        {
                            return Task.FromResult(1);
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ValueTaskFromResult()
    {
        var code = """
            namespace N
            {
                using System.IO;
                using System.Threading.Tasks;

                public class C
                {
                    public ValueTask<int> M()
                    {
                        using (var stream = File.OpenRead(string.Empty))
                        {
                            return ValueTask.FromResult(1);
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void TaskCompletedTask()
    {
        var code = """
            namespace N
            {
                using System.IO;
                using System.Threading.Tasks;

                public class C
                {
                    public Task M()
                    {
                        using (var stream = File.OpenRead(string.Empty))
                        {
                            return Task.CompletedTask;
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ValueTaskCompletedTask()
    {
        var code = """
            namespace N
            {
                using System.IO;
                using System.Threading.Tasks;

                public class C
                {
                    public ValueTask M()
                    {
                        using (var stream = File.OpenRead(string.Empty))
                        {
                            return ValueTask.CompletedTask;
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UsingNewMTaskRun()
    {
        var code = """
            namespace N
            {
                using System;
                using System.Threading.Tasks;

                public struct M : IDisposable
                {
                    public M(Task task)
                    {
                        this.Task = task;
                    }

                    public Task Task { get; }

                    public void Dispose()
                    {
                    }
                }

                public sealed class C
                {
                    public C()
                    {
                        using (new M(Task.Run(() => 1)))
                        {
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UsingNewMLocalTaskRun()
    {
        var code = """
            namespace N
            {
                using System;
                using System.Threading.Tasks;

                public struct M : IDisposable
                {
                    public M(Task task)
                    {
                        this.Task = task;
                    }

                    public Task Task { get; }

                    public void Dispose()
                    {
                    }
                }

                public sealed class C
                {
                    public C()
                    {
                        using (var bar = new M(Task.Run(() => 1)))
                        {
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UsingNewMLocalFuncTask()
    {
        var code = """
            namespace N
            {
                using System;
                using System.Threading.Tasks;

                public struct M : IDisposable
                {
                    public M(Func<Task> task)
                    {
                        this.Task = task();
                    }

                    public Task Task { get; }

                    public void Dispose()
                    {
                    }
                }

                public sealed class C
                {
                    public C()
                    {
                        var fromResult = Task.FromResult(1);
                        using (var bar = new M(() => fromResult))
                        {
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturnNullAfterAwaitIssue89()
    {
        var code = """
            #pragma warning disable SYSLIB0014
            namespace N
            {
                using System.Net;
                using System.Threading.Tasks;

                public class C
                {
                    public async Task<string?> M()
                    {
                        using (var client = new WebClient())
                        {
                            await Task.Delay(0);
                            return null;
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturnNullIssue89()
    {
        var code = """
            #pragma warning disable SYSLIB0014
            namespace N
            {
                using System.Net;
                using System.Threading.Tasks;

                public class C
                {
                    public Task<string>? M()
                    {
                        using (var client = new WebClient())
                        {
                            return null;
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void EarlyReturnNullIssue89()
    {
        var code = """
            namespace N
            {
                using System;
                using System.Net;
                using System.Net.Http;
                using System.Threading.Tasks;

                public class C
                {
                    private async Task<string?> Retrieve(HttpClient client, Uri location)
                    {
                        using (HttpResponseMessage response = await client.GetAsync(location))
                        {
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                return null;
                            }

                            return await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
            """;
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturnInValueTask()
    {
        var disposable = """
                namespace N;

                using System;

                public class Disposable : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

        var code = """

                namespace N;

                using System;
                using System.Threading.Tasks;

                public class C
                {
                    public ValueTask<IDisposable> M()
                    {
                        var disposable = new Disposable();
                        return new ValueTask<IDisposable>(disposable);
                    }
                }
                """;
        RoslynAssert.Valid(Analyzer, disposable, code);
    }

    [TestCase("default")]
    [TestCase("new ValueTask<int>(1)")]
    [TestCase("new ValueTask<int>(disposable.Equals(disposable) ? 1 : 0)")]
    [TestCase("new(1)")]
    [TestCase("new(disposable.Equals(disposable) ? 1 : 0)")]
    public static void ReturnNewValueTask(string expression)
    {
        var disposable = """
                namespace N;

                using System;

                public class Disposable : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

        var code = """

                namespace N;

                using System.Threading.Tasks;

                public class C
                {
                    public ValueTask<int> M1Async()
                    {
                      using (var disposable = new Disposable())
                      {
                        return new ValueTask<int>(1);
                      }
                    }
                }
                """.AssertReplace("new ValueTask<int>(1)", expression);
        RoslynAssert.Valid(Analyzer, disposable, code);
    }
}
