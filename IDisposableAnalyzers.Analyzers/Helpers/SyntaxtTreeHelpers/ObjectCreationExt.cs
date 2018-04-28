namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ObjectCreationExt
    {
        internal static bool Creates(this ObjectCreationExpressionSyntax creation, ConstructorDeclarationSyntax ctor, ReturnValueSearch search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var created = semanticModel.GetSymbolSafe(creation, cancellationToken) as IMethodSymbol;
            var ctorSymbol = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken);
            if (SymbolComparer.Equals(ctorSymbol, created))
            {
                return true;
            }

            return search == ReturnValueSearch.Recursive &&
                   Constructor.IsRunBefore(created, ctorSymbol, semanticModel, cancellationToken);
        }

        internal static bool TryGetMatchingArgument(this ObjectCreationExpressionSyntax objectCreation, IParameterSymbol parameter, out ArgumentSyntax argument)
        {
            if (objectCreation?.ArgumentList == null ||
                parameter == null)
            {
                argument = null;
                return false;
            }

            return objectCreation.TryFindArgument(parameter, out argument);
        }
    }
}
