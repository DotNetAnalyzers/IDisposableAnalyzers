namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static Result IsMemberDisposed(ISymbol member, TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsMemberDisposed(member, semanticModel.GetDeclaredSymbolSafe(context, cancellationToken), semanticModel, cancellationToken);
        }

        internal static Result IsMemberDisposed(ISymbol member, ITypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(member is IFieldSymbol ||
                  member is IPropertySymbol) ||
                  context == null)
            {
                return Result.Unknown;
            }

            using (var pooled = DisposeWalker.Borrow(context, semanticModel, cancellationToken))
            {
                return pooled.IsMemberDisposed(member);
            }
        }

        internal static bool IsMemberDisposed(ISymbol member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member == null ||
                disposeMethod == null)
            {
                return false;
            }

            using (var pooled = DisposeWalker.Borrow(disposeMethod, semanticModel, cancellationToken))
            {
                foreach (var invocation in pooled)
                {
                    if (DisposeCall.IsDisposing(invocation, member, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsDisposedBefore(ISymbol symbol, ExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            bool IsDisposing(InvocationExpressionSyntax invocation, ISymbol current)
            {
                if (invocation.TryGetMethodName(out var name) &&
                    name != "Dispose")
                {
                    return false;
                }

                var invokedSymbol = semanticModel.GetSymbolSafe(invocation, cancellationToken);
                if (invokedSymbol?.Name != "Dispose")
                {
                    return false;
                }

                var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
                if (statement != null)
                {
                    using (var names = IdentifierNameWalker.Borrow(statement))
                    {
                        foreach (var identifierName in names.IdentifierNames)
                        {
                            if (identifierName.Identifier.ValueText == current.Name &&
                                SymbolComparer.Equals(current, semanticModel.GetSymbolSafe(identifierName, cancellationToken)))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            bool TryGetScope(SyntaxNode node, out BlockSyntax result)
            {
                result = null;
                if (node.FirstAncestor<AnonymousFunctionExpressionSyntax>() is AnonymousFunctionExpressionSyntax lambda)
                {
                    result = lambda.Body as BlockSyntax;
                }
                else if (node.FirstAncestor<AccessorDeclarationSyntax>() is AccessorDeclarationSyntax accessor)
                {
                    result = accessor.Body;
                }
                else if (node.FirstAncestor<BaseMethodDeclarationSyntax>() is BaseMethodDeclarationSyntax method)
                {
                    result = method.Body;
                }

                return result != null;
            }

            if (TryGetScope(assignment, out var block))
            {
                using (var walker = InvocationWalker.Borrow(block))
                {
                    foreach (var invocation in walker.Invocations)
                    {
                        if (invocation.IsExecutedBefore(assignment) != Result.Yes)
                        {
                            continue;
                        }

                        if (IsDisposing(invocation, symbol))
                        {
                            return true;
                        }
                    }
                }
            }

            if (assignment is AssignmentExpressionSyntax assignmentExpression &&
                semanticModel.GetSymbolSafe(assignmentExpression.Left, cancellationToken) is IPropertySymbol property &&
                property.TryGetSetter(cancellationToken, out var setter))
            {
                using (var pooled = InvocationWalker.Borrow(setter))
                {
                    foreach (var invocation in pooled.Invocations)
                    {
                        if (IsDisposing(invocation, symbol) ||
                            IsDisposing(invocation, property))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
