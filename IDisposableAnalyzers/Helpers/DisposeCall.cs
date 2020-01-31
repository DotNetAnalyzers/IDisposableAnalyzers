namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeCall
    {
        internal static bool TryGetDisposed(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ISymbol? disposed)
        {
            disposed = null;
            return IsMatch(disposeCall, semanticModel, cancellationToken) &&
                   MemberPath.TrySingle(disposeCall, out var expression) &&
                   semanticModel.TryGetSymbol(expression, cancellationToken, out disposed);
        }

        internal static bool TryGetDisposedRootMember(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out IdentifierNameSyntax? disposedMember)
        {
            if (MemberPath.TryFindRoot(disposeCall, out var rootIdentifier) &&
               (disposedMember = rootIdentifier.Parent as IdentifierNameSyntax) is { })
            {
                switch (semanticModel.GetSymbolSafe(disposedMember, cancellationToken))
                {
                    case IPropertySymbol { GetMethod: null }:
                        return false;
                    case IPropertySymbol { GetMethod: { DeclaringSyntaxReferences: { Length: 1 } } getMethod }
                        when getMethod.TrySingleDeclaration(cancellationToken, out SyntaxNode? getterOrExpressionBody):
                        {
                            using var pooled = ReturnValueWalker.Borrow(getterOrExpressionBody, ReturnValueSearch.TopLevel, semanticModel, cancellationToken);
                            if (pooled.Count == 0)
                            {
                                return true;
                            }

                            return pooled.TrySingle(out var expression) &&
                                   MemberPath.TryFindRoot(expression, out rootIdentifier) &&
                                   (disposedMember = rootIdentifier.Parent as IdentifierNameSyntax) is { };
                        }

                    default:
                        return true;
                }
            }

            disposedMember = null;
            return false;
        }

        internal static bool IsDisposing(InvocationExpressionSyntax disposeCall, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetDisposed(disposeCall, semanticModel, cancellationToken, out var disposed))
            {
                if (disposed.Equals(symbol))
                {
                    return true;
                }

                if (disposed is IPropertySymbol property &&
                    property.TrySingleDeclaration(cancellationToken, out var declaration))
                {
                    using var walker = ReturnValueWalker.Borrow(declaration, ReturnValueSearch.TopLevel, semanticModel, cancellationToken);
                    return walker.TrySingle(out var returnValue) &&
                           MemberPath.TrySingle(returnValue, out var expression) &&
                           semanticModel.TryGetSymbol(expression, cancellationToken, out ISymbol? nested) &&
                           nested.Equals(symbol);
                }
            }

            return false;
        }

        internal static bool IsMatch(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return candidate.ArgumentList is { Arguments: { Count: 0 } } &&
                   (candidate.IsSymbol(KnownSymbol.IDisposable.Dispose, semanticModel, cancellationToken) ||
                    candidate.IsSymbol(KnownSymbol.IAsyncDisposable.DisposeAsync, semanticModel, cancellationToken));
        }
    }
}
