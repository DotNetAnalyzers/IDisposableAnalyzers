namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsIgnored(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.Parent is EqualsValueClauseSyntax equalsValueClause)
            {
                if (equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                    variableDeclarator.Identifier.Text == "_")
                {
                    return true;
                }

                return false;
            }

            if (node.Parent is AssignmentExpressionSyntax assignmentExpression)
            {
                return assignmentExpression.Left is IdentifierNameSyntax identifierName &&
                       identifierName.Identifier.Text == "_";
            }

            if (node.Parent is AnonymousFunctionExpressionSyntax ||
                node.Parent is UsingStatementSyntax ||
                node.Parent is ReturnStatementSyntax ||
                node.Parent is ArrowExpressionClauseSyntax)
            {
                return false;
            }

            if (node.Parent is StatementSyntax)
            {
                return true;
            }

            if (node.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax left &&
                left.Identifier.ValueText == "_")
            {
                return true;
            }

            if (node.Parent is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method))
            {
                if (method == KnownSymbol.CompositeDisposable.Add)
                {
                    return false;
                }

                if (method.Name == "Add" &&
                    method.ContainingType.IsAssignableTo(KnownSymbol.IEnumerable, semanticModel.Compilation))
                {
                    return false;
                }

                switch (IsDisposedByReturnValue(argument, semanticModel, cancellationToken))
                {
                    case Result.Yes:
                    case Result.AssumeYes:
                        return argumentList.Parent is ExpressionSyntax parentExpression &&
                               IsIgnored(parentExpression, semanticModel, cancellationToken);
                }

                if (IsAssignedToDisposable(argument, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes))
                {
                    return false;
                }

                return true;
            }

            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Parent is InvocationExpressionSyntax invocation &&
                    DisposeCall.IsIDisposableDispose(invocation, semanticModel, cancellationToken))
                {
                    return false;
                }

                return IsChainedDisposingInReturnValue(memberAccess, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is ConditionalAccessExpressionSyntax conditionalAccess)
            {
                if (conditionalAccess.WhenNotNull is InvocationExpressionSyntax invocation &&
                    DisposeCall.IsIDisposableDispose(invocation, semanticModel, cancellationToken))
                {
                    return false;
                }

                return IsChainedDisposingInReturnValue(conditionalAccess, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is InitializerExpressionSyntax initializer &&
                initializer.Parent is ExpressionSyntax creation)
            {
                return IsIgnored(creation, semanticModel, cancellationToken);
            }

            return false;
        }

        private static Result IsChainedDisposingInReturnValue(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
        {
            if (semanticModel.TryGetSymbol(memberAccess, cancellationToken, out ISymbol symbol))
            {
                return IsChainedDisposingInReturnValue(symbol, memberAccess, semanticModel, cancellationToken, visited);
            }

            return Result.Unknown;
        }

        private static Result IsChainedDisposingInReturnValue(ConditionalAccessExpressionSyntax conditionalAccess, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
        {
            if (semanticModel.TryGetSymbol(conditionalAccess.WhenNotNull, cancellationToken, out ISymbol symbol))
            {
                return IsChainedDisposingInReturnValue(symbol, conditionalAccess, semanticModel, cancellationToken, visited);
            }

            return Result.Unknown;
        }

        private static Result IsChainedDisposingInReturnValue(ISymbol symbol, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
        {
            if (symbol is IMethodSymbol method)
            {
                if (method.ReturnsVoid)
                {
                    return Result.No;
                }

                if (method.ReturnType.Name == "ConfiguredTaskAwaitable")
                {
                    return Result.Yes;
                }

                if (method.ContainingType.DeclaringSyntaxReferences.Length == 0)
                {
                    if (method.ReturnType == KnownSymbol.Task)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType == KnownSymbol.TaskOfT &&
                        method.ReturnType is INamedTypeSymbol namedType &&
                        namedType.TypeArguments.TrySingle(out var type))
                    {
                        return !IsAssignableFrom(type, semanticModel.Compilation)
                            ? Result.No
                            : Result.AssumeYes;
                    }

                    return !IsAssignableFrom(method.ReturnType, semanticModel.Compilation)
                        ? Result.No
                        : Result.AssumeYes;
                }

                if (method.IsExtensionMethod &&
                    method.ReducedFrom is IMethodSymbol reducedFrom &&
                    reducedFrom.Parameters.TryFirst(out var parameter))
                {
                    return IsDisposedByReturnValue(parameter, expression.Parent, semanticModel, cancellationToken, visited);
                }
            }

            return Result.AssumeNo;
        }
    }
}
