namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Property
    {
        internal static bool TryGetSetter(this IPropertySymbol property, CancellationToken cancellationToken, out AccessorDeclarationSyntax setter)
        {
            setter = null;
            return property.TrySingleDeclaration(cancellationToken, out var declaration) &&
                   declaration.TryGetSetter(out setter);
        }

        internal static bool IsAutoProperty(this IPropertySymbol property, CancellationToken cancellationToken)
        {
            return property.TrySingleDeclaration(cancellationToken, out var declaration) &&
                   declaration.IsAutoProperty();
        }
    }
}
