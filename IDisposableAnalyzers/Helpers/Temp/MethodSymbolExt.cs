namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MethodSymbolExt
    {
        internal static bool TryGetThisParameter(this IMethodSymbol method, out IParameterSymbol parameter)
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

        [Obsolete("Use Gu.Roslyn.Extensions")]
        internal static bool IsSymbol(this InvocationExpressionSyntax candidate, QualifiedMethod symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate is null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (candidate.TryGetMethodName(out var name) &&
                name != symbol.Name)
            {
                return false;
            }

            return semanticModel.TryGetSymbol(candidate, cancellationToken, out var candidateSymbol) &&
                   candidateSymbol == symbol;
        }
    }
}
