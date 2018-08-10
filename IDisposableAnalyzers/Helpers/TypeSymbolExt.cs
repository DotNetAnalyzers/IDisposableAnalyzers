namespace IDisposableAnalyzers
{
    using System;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    internal static class TypeSymbolExt
    {
        [Obsolete("Move to extensions.")]
        internal static bool IsAwaitable(this ITypeSymbol type)
        {
            return type.TryFindFirstMethod("GetAwaiter", x => x.Parameters.Length == 0, out var getAwaiter) &&
                   getAwaiter.ReturnType is ITypeSymbol returnType &&
                   returnType.TryFindFirstMethod("GetResult", x => x.Parameters.Length == 0, out _);
        }
    }
}
