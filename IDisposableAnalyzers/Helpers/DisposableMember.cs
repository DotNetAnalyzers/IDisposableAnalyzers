namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposableMember
    {
        internal static Result IsDisposed(FieldOrPropertyAndDeclaration member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsDisposed(member.FieldOrProperty, member.FieldOrProperty.ContainingType, semanticModel, cancellationToken);
        }

        internal static Result IsDisposed(FieldOrProperty member, INamedTypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (context is null)
            {
                return Result.Unknown;
            }

            using var walker = DisposeWalker.Borrow(context, semanticModel, cancellationToken);
            var isMemberDisposed = walker.IsMemberDisposed(member.Symbol);
            switch (isMemberDisposed)
            {
                case Result.Yes:
                case Result.AssumeYes:
                    return isMemberDisposed;
                default:
                    if (context.IsAssignableTo(KnownSymbol.SystemWindowsFormsForm, semanticModel.Compilation) &&
                        Winform.IsAddedToComponents(member, context, semanticModel, cancellationToken))
                    {
                        return Result.Yes;
                    }

                    return isMemberDisposed;
            }
        }

        internal static bool IsDisposed(FieldOrProperty member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposeMethod is null)
            {
                return false;
            }

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
                    if (DisposeCall.IsDisposing(candidate, member.Symbol, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var candidate in walker.Identifiers)
            {
                if (candidate.Identifier.Text == member.Name &&
                    semanticModel.TryGetSymbol(candidate, cancellationToken, out var candidateSymbol) &&
                    candidateSymbol.OriginalDefinition.Equals(member.Symbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
