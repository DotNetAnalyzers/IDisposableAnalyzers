namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ConstructorExt
    {
        internal static bool Initializes(this ConstructorDeclarationSyntax candidate, IMethodSymbol target, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate.Initializer is { } initializer &&
                semanticModel.TryGetSymbol(initializer, cancellationToken, out var initialized))
            {
                if (MethodSymbolComparer.Equal(initialized, target))
                {
                    return true;
                }

                if (initialized.ContainingType.IsAssignableTo(target.ContainingType, semanticModel.Compilation) &&
                    initialized.TrySingleDeclaration<ConstructorDeclarationSyntax>(cancellationToken, out var recursive))
                {
                    return Initializes(recursive, target, semanticModel, cancellationToken);
                }
            }

            return false;
        }
    }
}
