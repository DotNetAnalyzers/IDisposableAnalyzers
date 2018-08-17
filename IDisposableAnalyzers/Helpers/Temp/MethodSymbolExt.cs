namespace IDisposableAnalyzers
{
    using System;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    internal static class MethodSymbolExt
    {
        [Obsolete("Move to Gu.Extensions")]
        internal static bool TryGetThisParameter(this IMethodSymbol method, out IParameterSymbol parameter)
        {
            if (method.IsExtensionMethod)
            {
                if (method.ReducedFrom is IMethodSymbol reduced)
                {
                    return reduced.Parameters.TryFirst(out parameter);
                }

                return method.Parameters.TryFirst(out parameter);
            }

            parameter = null;
            return false;
        }
    }
}
