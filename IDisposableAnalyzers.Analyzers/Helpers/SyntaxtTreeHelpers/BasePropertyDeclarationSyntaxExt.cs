namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BasePropertyDeclarationSyntaxExt
    {
        internal static bool IsPropertyOrIndexer(this BasePropertyDeclarationSyntax declaration)
        {
            return declaration is PropertyDeclarationSyntax || declaration is IndexerDeclarationSyntax;
        }

        internal static bool TryGetGetAccessorDeclaration(this BasePropertyDeclarationSyntax property, out AccessorDeclarationSyntax result)
        {
            return TryGetAccessorDeclaration(property, SyntaxKind.GetAccessorDeclaration, out result);
        }

        internal static bool TryGetSetAccessorDeclaration(this BasePropertyDeclarationSyntax property, out AccessorDeclarationSyntax result)
        {
            return TryGetAccessorDeclaration(property, SyntaxKind.SetAccessorDeclaration, out result);
        }

        internal static bool TryGetAccessorDeclaration(this BasePropertyDeclarationSyntax property, SyntaxKind kind, out AccessorDeclarationSyntax result)
        {
            result = null;
            var accessors = property?.AccessorList?.Accessors;
            if (accessors == null)
            {
                return false;
            }

            foreach (var accessor in accessors.Value)
            {
                if (accessor.IsKind(kind))
                {
                    result = accessor;
                    return true;
                }
            }

            return false;
        }
    }
}