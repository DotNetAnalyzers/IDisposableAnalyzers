namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeCall
    {
        internal static bool TryGetDisposed(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol disposed)
        {
            disposed = null;
            if (IsIDisposableDispose(disposeCall))
            {
                throw new NotImplementedException("Use MemberPat.TrySingle here.");
                using (var walker = Gu.Roslyn.AnalyzerExtensions.MemberPath.PathWalker.Borrow(disposeCall))
                {
                    if (walker.IdentifierNames.TrySingle(out var expression) &&
                        semanticModel.TryGetSymbol(expression, cancellationToken, out disposed))
                    {
                        return true;
                    }
                }
            }

            return false;
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
                        throw new NotImplementedException("Use MemberPat.TrySingle here.");
                        return walker.TrySingle(out var expression) &&
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
