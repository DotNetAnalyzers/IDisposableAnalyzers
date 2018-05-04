namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposableMember
    {
        internal static Result IsDisposed(ISymbol member, TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsDisposed(member, semanticModel.GetDeclaredSymbolSafe(context, cancellationToken), semanticModel, cancellationToken);
        }

        internal static Result IsDisposed(ISymbol member, ITypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(member is IFieldSymbol ||
                  member is IPropertySymbol) ||
                context == null)
            {
                return Result.Unknown;
            }

            using (var walker = DisposeWalker.Borrow(context, semanticModel, cancellationToken))
            {
                return walker.IsMemberDisposed(member);
            }
        }

        internal static bool IsDisposed(ISymbol member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member == null ||
                disposeMethod == null)
            {
                return false;
            }

            using (var walker = DisposeWalker.Borrow(disposeMethod, semanticModel, cancellationToken))
            {
                foreach (var invocation in walker)
                {
                    if (DisposeCall.IsDisposing(invocation, member, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
