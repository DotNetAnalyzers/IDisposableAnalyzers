// ReSharper disable UnusedMember.Global
namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class SymbolExt
    {
        internal static bool IsEither<T1, T2>(this ISymbol symbol)
            where T1 : ISymbol
            where T2 : ISymbol
        {
            return symbol is T1 || symbol is T2;
        }

        internal static bool TryGetScope(this ILocalSymbol local, CancellationToken cancellationToken, out SyntaxNode scope)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration))
            {
                if (declaration.FirstAncestor<AnonymousFunctionExpressionSyntax>() is SyntaxNode lambda)
                {
                    scope = lambda;
                    return true;
                }

                if (declaration.FirstAncestor<MemberDeclarationSyntax>() is SyntaxNode member)
                {
                    scope = member;
                    return true;
                }
            }

            scope = null;
            return false;
        }
    }
}
