namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Property
    {
        internal static bool AssignsSymbolInSetter(IPropertySymbol property, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var setMethod = property?.SetMethod;
            if (setMethod == null ||
                setMethod.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            if (TryGetSetter(property, cancellationToken, out var setter))
            {
                if (AssignmentWalker.FirstForSymbol(symbol, setter, Search.Recursive, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetSetter(this IPropertySymbol property, CancellationToken cancellationToken, out AccessorDeclarationSyntax setter)
        {
            setter = null;
            if (property == null)
            {
                return false;
            }

            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                var propertyDeclaration = reference.GetSyntax(cancellationToken) as PropertyDeclarationSyntax;
                if (propertyDeclaration.TryGetSetAccessorDeclaration(out setter))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsAutoProperty(this IPropertySymbol propertySymbol, CancellationToken cancellationToken)
        {
            if (propertySymbol == null)
            {
                return false;
            }

            foreach (var reference in propertySymbol.DeclaringSyntaxReferences)
            {
                var declaration = (BasePropertyDeclarationSyntax)reference.GetSyntax(cancellationToken);
                if ((declaration as PropertyDeclarationSyntax)?.ExpressionBody != null)
                {
                    return false;
                }

                if (declaration.TryGetGetAccessorDeclaration(out var getter) &&
                    getter.Body == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
