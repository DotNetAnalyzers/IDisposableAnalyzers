namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Microsoft.CodeAnalysis;

    public static partial class Valid
    {
        private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new();
        private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP006ImplementIDisposable;

        private const string Disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
    }
}
