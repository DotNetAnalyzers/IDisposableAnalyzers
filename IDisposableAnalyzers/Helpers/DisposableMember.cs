namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposableMember
    {
        internal static Result IsDisposed(FieldOrProperty member, TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsDisposed(member, semanticModel.GetDeclaredSymbolSafe(context, cancellationToken), semanticModel, cancellationToken);
        }

        internal static Result IsDisposed(FieldOrProperty member, ITypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                return Result.Unknown;
            }

            using (var walker = DisposeWalker.Borrow(context, semanticModel, cancellationToken))
            {
                return walker.IsMemberDisposed(member.Symbol);
            }
        }

        internal static bool IsDisposed(FieldOrProperty member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposeMethod == null)
            {
                return false;
            }

            if (disposeMethod.TrySingleMethodDeclaration(cancellationToken, out var disposeMethodDeclaration))
            {
                using (var walker = DisposeWalker.Borrow(disposeMethodDeclaration, semanticModel, cancellationToken))
                {
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
                            semanticModel.TryGetSymbol(candidate, cancellationToken, out ISymbol candidateSymbol) &&
                            candidateSymbol.OriginalDefinition.Equals(member.Symbol))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;
        }
    }
}
