namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class SymbolExt
    {
        internal static bool IsEither<T1, T2>(this ISymbol symbol)
            where T1 : ISymbol
            where T2 : ISymbol
        {
            return symbol is T1 || symbol is T2;
        }

        internal static bool TrySingleDeclaration<T>(this ISymbol symbol, CancellationToken cancellationToken, out T declaration)
            where T : SyntaxNode
        {
            declaration = null;
            if (symbol == null)
            {
                return false;
            }

            if (symbol.DeclaringSyntaxReferences.TrySingle(out var reference))
            {
                var syntax = reference.GetSyntax(cancellationToken);
                if (symbol is IFieldSymbol &&
                    syntax is VariableDeclaratorSyntax declarator)
                {
                    syntax = declarator.FirstAncestor<FieldDeclarationSyntax>();
                }

                declaration = syntax as T;
                return declaration != null;
            }

            return false;
        }
    }
}
