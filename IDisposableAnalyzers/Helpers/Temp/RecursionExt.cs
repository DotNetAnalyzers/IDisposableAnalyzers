namespace IDisposableAnalyzers
{
    using System;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Obsolete("Use Gu.Roslyn.Extensions")]
    internal static class RecursionExt
    {
        internal static SymbolAndDeclaration<ILocalSymbol, SyntaxNode>? Target(this Recursion recursion, VariableDeclaratorSyntax variableDeclarator)
        {
            if (recursion.SemanticModel.TryGetSymbol(variableDeclarator, recursion.CancellationToken, out ILocalSymbol? local) &&
                local.TryGetScope(recursion.CancellationToken, out var scope))
            {
                return new SymbolAndDeclaration<ILocalSymbol, SyntaxNode>(local, scope);
            }

            return null;
        }
    }
}
