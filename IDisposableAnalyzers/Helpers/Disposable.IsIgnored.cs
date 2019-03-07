namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsIgnored(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
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

            if (node.Parent is ArgumentSyntax argument)
            {
#pragma warning disable IDISP003 // Dispose previous before re - assigning.
                using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                {
                    if (visited.Add(argument))
                    {
                        return IsIgnored(argument, semanticModel, cancellationToken, visited);
                    }
                }
            }

            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Parent is InvocationExpressionSyntax invocation &&
                    DisposeCall.IsIDisposableDispose(invocation, semanticModel, cancellationToken))
                {
                    return false;
                }

                return IsChainedDisposingInReturnValue(memberAccess, semanticModel, cancellationToken, visited).IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is ConditionalAccessExpressionSyntax conditionalAccess)
            {
                if (conditionalAccess.WhenNotNull is InvocationExpressionSyntax invocation &&
                    DisposeCall.IsIDisposableDispose(invocation, semanticModel, cancellationToken))
                {
                    return false;
                }

                return IsChainedDisposingInReturnValue(conditionalAccess, semanticModel, cancellationToken, visited).IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is InitializerExpressionSyntax initializer &&
                initializer.Parent is ExpressionSyntax creation)
            {
#pragma warning disable IDISP003 // Dispose previous before re - assigning.
                using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                {
                    if (visited.Add(creation))
                    {
                        return IsIgnored(creation, semanticModel, cancellationToken, visited);
                    }

                    return false;
                }
            }

            return false;
        }

        internal static bool IsIgnored(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            if (argument != null &&
                argument.Parent is ArgumentListSyntax argumentList &&
                argumentList.Parent is ExpressionSyntax parentExpression &&
                semanticModel.TryGetSymbol(parentExpression, cancellationToken, out IMethodSymbol method))
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

                if (TryFindParameter(out var parameter) &&
                    method.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax methodDeclaration))
                {
                    using (var walker = IdentifierNameWalker.Borrow(methodDeclaration))
                    {
                        walker.RemoveAll(x => !IsMatch(x));
                        if (walker.IdentifierNames.Count == 0)
                        {
                            return true;
                        }

                        return walker.IdentifierNames.All(x => IsIgnored(x));

                        bool IsMatch(IdentifierNameSyntax candidate)
                        {
                            if (candidate.Identifier.Text != parameter.Name)
                            {
                                return false;
                            }

                            return semanticModel.TryGetSymbol<IParameterSymbol>(candidate, cancellationToken, out _);
                        }

                        bool IsIgnored(IdentifierNameSyntax candidate)
                        {
                            switch (candidate.Parent.Kind())
                            {
                                case SyntaxKind.NotEqualsExpression:
                                    return true;
                            }

                            switch (candidate.Parent)
                            {
                                case AssignmentExpressionSyntax assignment when assignment.Right == candidate &&
                                                                                semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol assignedSymbol) &&
                                                                                FieldOrProperty.TryCreate(assignedSymbol, out var assignedMember):
                                    if (DisposeMethod.TryFindFirst(assignedMember.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
                                        DisposableMember.IsDisposed(assignedMember, disposeMethod, semanticModel, cancellationToken))
                                    {
                                        return Disposable.IsIgnored(parentExpression, semanticModel, cancellationToken, visited);
                                    }

                                    return !semanticModel.IsAccessible(argument.SpanStart, assignedMember.Symbol);
                                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator:
                                    if (variableDeclarator.TryFirstAncestor<UsingStatementSyntax>(out _))
                                    {
                                        return false;
                                    }

                                    using (var invocations = InvocationWalker.Borrow(methodDeclaration))
                                    {
                                        // Just checking if there is a dispose call in the scope for now.
                                        if (invocations.TryFirst(x => DisposeCall.IsIDisposableDispose(x, semanticModel, cancellationToken), out _))
                                        {
                                            return false;
                                        }
                                    }

                                    break;
                                    // return Disposable.IsIgnored(variableDeclarator, semanticModel, cancellationToken, visited);
                            }

                            if (Disposable.IsIgnored(candidate, semanticModel, cancellationToken, visited))
                            {
                                return true;
                            }

                            return false;
                        }
                    }
                }
                else
                {
                    if (TryGetAssignedFieldOrProperty(argument, method, semanticModel, cancellationToken, out var assignedMember))
                    {
                        return !semanticModel.IsAccessible(argument.SpanStart, assignedMember.Symbol);
                    }
                }

                return false;
            }

            return false;

            // https://github.com/GuOrg/Gu.Roslyn.Extensions/issues/40
            bool TryFindParameter(out IParameterSymbol result)
            {
                return method.TryFindParameter(argument, out result) ||
                       (method.Parameters.TryLast(out result) &&
                        result.IsParams);
            }
        }

        private static Result IsChainedDisposingInReturnValue(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            if (semanticModel.TryGetSymbol(memberAccess, cancellationToken, out ISymbol symbol))
            {
                return IsChainedDisposingInReturnValue(symbol, memberAccess, semanticModel, cancellationToken, visited);
            }

            return Result.Unknown;
        }

        private static Result IsChainedDisposingInReturnValue(ConditionalAccessExpressionSyntax conditionalAccess, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            if (semanticModel.TryGetSymbol(conditionalAccess.WhenNotNull, cancellationToken, out ISymbol symbol))
            {
                return IsChainedDisposingInReturnValue(symbol, conditionalAccess, semanticModel, cancellationToken, visited);
            }

            return Result.Unknown;
        }

        private static Result IsChainedDisposingInReturnValue(ISymbol symbol, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
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
