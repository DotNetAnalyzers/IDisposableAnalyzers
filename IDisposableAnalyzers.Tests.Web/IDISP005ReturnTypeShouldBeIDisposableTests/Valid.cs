namespace IDisposableAnalyzers.Tests.Web.IDISP005ReturnTypeShouldBeIDisposableTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

public static class Valid
{
    private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP005ReturnTypeShouldBeIDisposable;

    [Test]
    public static void LocalDisposeAsync()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public IAsyncDisposable M() => File.OpenRead(string.Empty);
    }
}";

        RoslynAssert.Valid(Analyzer, Descriptor, code);
    }
}
