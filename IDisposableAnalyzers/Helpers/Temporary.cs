namespace IDisposableAnalyzers;

using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Temporary
{
    internal static IMethodSymbol? GetSymbolSafe(this SemanticModel semanticModel, InvocationExpressionSyntax node, CancellationToken cancellationToken)
    {
        if (semanticModel is null)
        {
            throw new System.ArgumentNullException(nameof(semanticModel));
        }

        if (node is null)
        {
            throw new System.ArgumentNullException(nameof(node));
        }

        return semanticModel.GetSymbolSafe((SyntaxNode)node, cancellationToken) as IMethodSymbol;
    }

    internal static bool TryGetSymbol(this SemanticModel semanticModel, InvocationExpressionSyntax node, CancellationToken cancellationToken, [NotNullWhen(true)] out IMethodSymbol? symbol)
    {
        if (semanticModel is null)
        {
            throw new System.ArgumentNullException(nameof(semanticModel));
        }

        if (node is null)
        {
            throw new System.ArgumentNullException(nameof(node));
        }

        symbol = semanticModel.GetSymbolSafe((SyntaxNode)node, cancellationToken) as IMethodSymbol;
        return symbol is { };
    }
}
