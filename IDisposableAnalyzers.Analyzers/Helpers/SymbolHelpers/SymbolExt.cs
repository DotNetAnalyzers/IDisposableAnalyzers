// ReSharper disable UnusedMember.Global
namespace IDisposableAnalyzers
{
    using System.Diagnostics;
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

        internal static bool TrySingleDeclaration(this IFieldSymbol field, CancellationToken cancellationToken, out FieldDeclarationSyntax declaration)
        {
            declaration = null;
            if (field != null &&
                field.DeclaringSyntaxReferences.TrySingle(out var reference))
            {
                declaration = reference.GetSyntax(cancellationToken).FirstAncestorOrSelf<FieldDeclarationSyntax>();
            }

            return declaration != null;
        }

        internal static bool TrySingleDeclaration(this IPropertySymbol property, CancellationToken cancellationToken, out PropertyDeclarationSyntax declaration)
        {
            declaration = null;
            if (property != null &&
                property.DeclaringSyntaxReferences.TrySingle(out var reference))
            {
                declaration = reference.GetSyntax(cancellationToken) as PropertyDeclarationSyntax;
            }

            return declaration != null;
        }

        internal static bool TrySingleDeclaration(this IMethodSymbol method, CancellationToken cancellationToken, out BaseMethodDeclarationSyntax declaration)
        {
            declaration = null;
            if (method != null &&
                method.DeclaringSyntaxReferences.TrySingle(out var reference))
            {
                Debug.Assert(method.AssociatedSymbol == null, "method.AssociatedSymbol == null");
                declaration = reference.GetSyntax(cancellationToken) as BaseMethodDeclarationSyntax;
            }

            return declaration != null;
        }

        internal static bool TrySingleDeclaration(this IParameterSymbol parameter, CancellationToken cancellationToken, out ParameterSyntax declaration)
        {
            declaration = null;
            if (parameter != null &&
                parameter.DeclaringSyntaxReferences.TrySingle(out var reference))
            {
                declaration = reference.GetSyntax(cancellationToken) as ParameterSyntax;
            }

            return declaration != null;
        }

        internal static bool TrySingleDeclaration(this ILocalSymbol local, CancellationToken cancellationToken, out VariableDeclarationSyntax declaration)
        {
            declaration = null;
            if (local != null &&
                local.DeclaringSyntaxReferences.TrySingle(out var reference))
            {
                declaration = reference.GetSyntax(cancellationToken).FirstAncestorOrSelf<VariableDeclarationSyntax>();
            }

            return declaration != null;
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
                if (syntax is VariableDeclaratorSyntax declarator &&
                    symbol.IsEither<IFieldSymbol, ILocalSymbol>())
                {
                    syntax = declarator.FirstAncestorOrSelf<T>();
                }

                declaration = syntax as T;
                return declaration != null;
            }

            return false;
        }
    }
}
