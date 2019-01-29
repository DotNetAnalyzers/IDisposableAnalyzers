namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsDisposedBefore(ISymbol symbol, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetScope(expression, out var block))
            {
                using (var walker = InvocationWalker.Borrow(block))
                {
                    foreach (var invocation in walker.Invocations)
                    {
                        if (invocation.IsExecutedBefore(expression) == ExecutedBefore.No)
                        {
                            continue;
                        }

                        if (DisposeCall.IsDisposing(invocation, symbol, semanticModel, cancellationToken) &&
                            !IsReassignedAfter(block, invocation))
                        {
                            return true;
                        }
                    }
                }
            }

            if (expression is AssignmentExpressionSyntax assignmentExpression &&
                semanticModel.GetSymbolSafe(assignmentExpression.Left, cancellationToken) is IPropertySymbol property &&
                property.TryGetSetter(cancellationToken, out var setter))
            {
                using (var pooled = InvocationWalker.Borrow(setter))
                {
                    foreach (var invocation in pooled.Invocations)
                    {
                        if ((DisposeCall.IsDisposing(invocation, symbol, semanticModel, cancellationToken) ||
                             DisposeCall.IsDisposing(invocation, property, semanticModel, cancellationToken)) &&
                             !IsReassignedAfter(setter, invocation))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;

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

            bool IsReassignedAfter(SyntaxNode scope, InvocationExpressionSyntax disposeCall)
            {
                using (var walker = MutationWalker.Borrow(scope, Scope.Member, semanticModel, cancellationToken))
                {
                    foreach (var mutation in walker.All())
                    {
                        if (mutation.TryFirstAncestor(out StatementSyntax statement) &&
                            disposeCall.IsExecutedBefore(statement) == ExecutedBefore.Yes &&
                            statement.IsExecutedBefore(expression) == ExecutedBefore.Yes)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        internal static bool IsDisposedAfter(ISymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location.FirstAncestorOrSelf<MemberDeclarationSyntax>() is MemberDeclarationSyntax scope)
            {
                using (var walker = InvocationWalker.Borrow(scope))
                {
                    foreach (var invocation in walker.Invocations)
                    {
                        if (location.IsExecutedBefore(invocation).IsEither(ExecutedBefore.Maybe, ExecutedBefore.Yes) &&
                            DisposeCall.IsDisposing(invocation, local, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }

                using (var walker = UsingStatementWalker.Borrow(scope))
                {
                    foreach (var usingStatement in walker.UsingStatements)
                    {
                        if (location.IsExecutedBefore(usingStatement) == ExecutedBefore.Yes &&
                            usingStatement.Expression is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == local.Name)
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
