namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal static bool IsType(this ObjectCreationExpressionSyntax objectCreation, QualifiedType type, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return objectCreation switch
            {
                { Type: IdentifierNameSyntax identifierName } => identifierName == type &&
                                                                 semanticModel.TryGetType(objectCreation, cancellationToken, out var symbol) &&
                                                                 symbol == type,
                _ => semanticModel.TryGetType(objectCreation, cancellationToken, out var symbol) &&
                     symbol == type,
            };
        }
    }
}
