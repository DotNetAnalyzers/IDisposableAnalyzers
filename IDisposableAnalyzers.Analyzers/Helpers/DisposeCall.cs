namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeCall
    {
        internal static bool TryGetDisposed(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol disposed)
        {
            disposed = null;
            return IsIDisposableDispose(disposeCall) &&
                   MemberPath.TrySingle(disposeCall, out var expression) &&
                   semanticModel.TryGetSymbol(expression, cancellationToken, out disposed);
        }

        internal static bool IsDisposing(InvocationExpressionSyntax disposeCall, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetDisposed(disposeCall, semanticModel, cancellationToken, out var disposed))
            {
                if (SymbolComparer.Equals(disposed, symbol))
                {
                    return true;
                }

                if (disposed is IPropertySymbol property &&
                    property.TrySingleDeclaration(cancellationToken, out var declaration))
                {
                    using (var walker = ReturnValueWalker.Borrow(declaration, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                    {
                        return walker.TrySingle(out var returnValue) &&
                               MemberPath.TrySingle(returnValue, out var expression) &&
                               semanticModel.TryGetSymbol(expression, cancellationToken, out ISymbol nested) &&
                               SymbolComparer.Equals(nested, symbol);
                    }
                }
            }

            return false;
        }

        internal static bool IsIDisposableDispose(InvocationExpressionSyntax candidate)
        {
            return candidate.TryGetMethodName(out var name) &&
                   name == "Dispose" &&
                   candidate.ArgumentList is ArgumentListSyntax argumentList &&
                   argumentList.Arguments.Count == 0 &&
                   candidate.Expression != null;
        }
    }
}
