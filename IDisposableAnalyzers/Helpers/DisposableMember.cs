namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposableMember
    {
        internal static bool IsDisposed(FieldOrPropertyAndDeclaration member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsDisposed(member.FieldOrProperty, member.FieldOrProperty.ContainingType, semanticModel, cancellationToken);
        }

        internal static bool IsDisposed(FieldOrProperty member, INamedTypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using var walker = DisposeWalker.Borrow(context, semanticModel, cancellationToken);
            if (walker.IsMemberDisposed(member.Symbol))
            {
                return true;
            }

            if (context.IsAssignableTo(KnownSymbol.SystemWindowsFormsForm, semanticModel.Compilation) &&
                Winform.IsAddedToComponents(member, context, semanticModel, cancellationToken))
            {
                return true;
            }

            return false;
        }

        internal static bool IsDisposed(FieldOrProperty member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposeMethod.TrySingleMethodDeclaration(cancellationToken, out var disposeMethodDeclaration))
            {
                return IsDisposed(member, disposeMethodDeclaration, semanticModel, cancellationToken);
            }

            return false;
        }

        internal static bool IsDisposed(FieldOrProperty member, MethodDeclarationSyntax disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using var walker = DisposeWalker.Borrow(disposeMethod, semanticModel, cancellationToken);
            if (Disposable.IsAssignableFrom(member.Type, semanticModel.Compilation))
            {
                foreach (var candidate in walker.Invocations)
                {
                    if (DisposeCall.MatchAny(candidate, semanticModel, cancellationToken) is { } dispose &&
                        dispose.IsDisposing(member.Symbol, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            foreach (var candidate in walker.Identifiers)
            {
                if (candidate.Identifier.Text == member.Name &&
                    semanticModel.TryGetSymbol(candidate, cancellationToken, out var candidateSymbol) &&
                   SymbolComparer.Equal(candidateSymbol.OriginalDefinition, member.Symbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
