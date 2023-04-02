namespace IDisposableAnalyzers;

using System.Diagnostics.CodeAnalysis;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;

internal static class MethodSymbolExt
{
    internal static bool TryGetThisParameter(this IMethodSymbol method, [NotNullWhen(true)] out IParameterSymbol? parameter)
    {
        if (method.IsExtensionMethod)
        {
            if (method.ReducedFrom is { } reduced)
            {
                return reduced.Parameters.TryFirst(out parameter);
            }

            return method.Parameters.TryFirst(out parameter);
        }

        parameter = null;
        return false;
    }
}
