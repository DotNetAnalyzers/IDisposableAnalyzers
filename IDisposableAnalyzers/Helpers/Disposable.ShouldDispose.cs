namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    internal static partial class Disposable
    {
        internal static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.Symbol is IParameterSymbol parameter &&
                parameter.RefKind != RefKind.None)
            {
                return false;
            }

            if (localOrParameter.Type.IsAssignableTo(KnownSymbols.Task, semanticModel.Compilation))
            {
                return false;
            }

            using var recursion = Recursion.Borrow(localOrParameter.Symbol.ContainingType, semanticModel, cancellationToken);
            using var walker = UsagesWalker.Borrow(localOrParameter, semanticModel, cancellationToken);
            foreach (var usage in walker.Usages)
            {
                if (Returns(usage, recursion))
                {
                    return false;
                }

                if (Assigns(usage, recursion, out _))
                {
                    return false;
                }

                if (Stores(usage, recursion, out _))
                {
                    return false;
                }

                if (Disposes(usage, recursion))
                {
                    return false;
                }

                if (DisposedByReturnValue(usage, recursion, out _))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
