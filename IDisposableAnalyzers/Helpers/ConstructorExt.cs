namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class ConstructorExt
{
    internal static bool Initializes(this ConstructorDeclarationSyntax candidate, IMethodSymbol target, Recursion recursion)
    {
        if (candidate.Initializer is { } initializer &&
            recursion.Target(initializer) is { Symbol: { } initialized, Declaration: { } recursive })
        {
            if (MethodSymbolComparer.Equal(initialized, target))
            {
                return true;
            }

            if (initialized.ContainingType.IsAssignableTo(target.ContainingType, recursion.SemanticModel.Compilation))
            {
                return Initializes(recursive, target, recursion);
            }
        }

        return false;
    }
}
