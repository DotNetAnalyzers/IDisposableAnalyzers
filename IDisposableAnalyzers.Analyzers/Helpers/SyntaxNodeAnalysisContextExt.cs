namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal static class SyntaxNodeAnalysisContextExt
    {
        internal static bool IsExcludedFromAnalysis(this SyntaxNodeAnalysisContext context)
        {
            if (context.Node == null ||
                context.Node.IsMissing ||
                context.SemanticModel == null)
            {
                return true;
            }

            return context.SemanticModel.SyntaxTree.FilePath.EndsWith(".g.cs");
        }

        internal static IPropertySymbol ContainingProperty(this SyntaxNodeAnalysisContext context)
        {
            var containingSymbol = context.ContainingSymbol;
            if (containingSymbol is IPropertySymbol propertySymbol)
            {
                return propertySymbol;
            }

            return (containingSymbol as IMethodSymbol)?.AssociatedSymbol as IPropertySymbol;
        }
    }
}