namespace IDisposableAnalyzers
{
    using System.Linq;
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
                node.Parent is EqualsValueClauseSyntax ||
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

            if (node.Parent is ArgumentSyntax argument)
            {
                if (argument.Parent is ArgumentListSyntax argumentList &&
                    argumentList.Parent is InvocationExpressionSyntax invocation &&
                    semanticModel.TryGetSymbol(invocation, cancellationToken, out var method))
                {
                    if (method == KnownSymbol.CompositeDisposable.Add)
                    {
                        return false;
                    }

                    if (method.Name == "Add" &&
                        method.ContainingType.IsAssignableTo(KnownSymbol.IEnumerable, semanticModel.Compilation))
                    {
                        if (!method.ContainingType.TypeArguments.Any(x => x.IsAssignableTo(KnownSymbol.IDisposable, semanticModel.Compilation)))
                        {
                            if (MemberPath.TryFindRoot(invocation, out var identifierName) &&
                                semanticModel.TryGetSymbol(identifierName, cancellationToken, out ISymbol symbol) &&
                                FieldOrProperty.TryCreate(symbol, out var fieldOrProperty) &&
                                argument.TryFirstAncestor(out TypeDeclarationSyntax typeDeclaration) &&
                                DisposableMember.IsDisposed(fieldOrProperty, typeDeclaration, semanticModel, cancellationToken) != Result.No)
                            {
                                return false;
                            }

                            return true;
                        }

                        return false;
                    }
                }

                return IsDisposedByReturnValue(argument, semanticModel, cancellationToken)
                           .IsEither(Result.No, Result.AssumeNo) &&
                       IsAssignedToDisposable(argument, semanticModel, cancellationToken)
                           .IsEither(Result.No, Result.AssumeNo);
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
                    return CheckReturnValues(parameter, expression.Parent, semanticModel, cancellationToken, visited);
                }
            }

            return Result.AssumeNo;
        }
    }
}
