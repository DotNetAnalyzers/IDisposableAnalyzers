namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Ignores(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (Disposes(node, semanticModel, cancellationToken, visited))
            {
                return false;
            }

            switch (node.Parent)
            {
                case EqualsValueClauseSyntax equalsValueClause:
                    {
                        if (equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                            variableDeclarator.Identifier.Text == "_")
                        {
                            return true;
                        }

                        return false;
                    }

                case AssignmentExpressionSyntax assignmentExpression:
                    {
                        return assignmentExpression.Left is IdentifierNameSyntax identifierName &&
                               identifierName.Identifier.Text == "_";
                    }

                case AnonymousFunctionExpressionSyntax _:
                case UsingStatementSyntax _:
                case ReturnStatementSyntax _:
                case ArrowExpressionClauseSyntax _:
                    return false;
                case StatementSyntax _:
                    return true;
                case ArgumentSyntax argument:
                    if (visited.CanVisit(argument, out visited))
                    {
                        using (visited)
                        {
                            return Ignores(argument, semanticModel, cancellationToken, visited);
                        }
                    }

                    break;
                case MemberAccessExpressionSyntax memberAccess when semanticModel.TryGetSymbol(memberAccess, cancellationToken, out var symbol):
                    return IsChainedDisposingInReturnValue(symbol, semanticModel, cancellationToken, visited).IsEither(Result.No, Result.AssumeNo);
                case ConditionalAccessExpressionSyntax conditionalAccess when semanticModel.TryGetSymbol(conditionalAccess.WhenNotNull, cancellationToken, out var symbol):
                    return IsChainedDisposingInReturnValue(symbol, semanticModel, cancellationToken, visited).IsEither(Result.No, Result.AssumeNo);

                case InitializerExpressionSyntax initializer when initializer.Parent is ExpressionSyntax creation:
                    if (visited.CanVisit(creation, out visited))
                    {
                        using (visited)
                        {
                            return Ignores(creation, semanticModel, cancellationToken, visited);
                        }
                    }

                    break;
            }

            return false;
        }

        private static bool Ignores(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
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

                if (method.DeclaringSyntaxReferences.IsEmpty)
                {
                    if (TryGetAssignedFieldOrProperty(argument, method, semanticModel, cancellationToken, out var assignedMember))
                    {
                        return !Disposable.IsAssignableFrom(assignedMember.Type, semanticModel.Compilation) ||
                               !semanticModel.IsAccessible(argument.SpanStart, assignedMember.Symbol);
                    }

                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        return !Disposable.IsAssignableFrom(method.ContainingType, semanticModel.Compilation);
                    }

                    return !Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation);
                }

                if (method.TryFindParameter(argument, out var parameter) &&
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
                                case SyntaxKind.Argument:
                                    // Stopping analysis here assuming it is handled
                                    return false;
                            }

                            switch (candidate.Parent)
                            {
                                case AssignmentExpressionSyntax assignment when assignment.Right == candidate &&
                                                                                semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol assignedSymbol) &&
                                                                                FieldOrProperty.TryCreate(assignedSymbol, out var assignedMember):
                                    if (DisposeMethod.TryFindFirst(assignedMember.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
                                        DisposableMember.IsDisposed(assignedMember, disposeMethod, semanticModel, cancellationToken))
                                    {
                                        return DisposableWalker.Ignores(parentExpression, semanticModel, cancellationToken, visited);
                                    }

                                    if (parentExpression.Parent.IsEither(SyntaxKind.ArrowExpressionClause, SyntaxKind.ReturnStatement))
                                    {
                                        return true;
                                    }

                                    return !semanticModel.IsAccessible(argument.SpanStart, assignedMember.Symbol);
                                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator:
                                    return DisposableWalker.Ignores(variableDeclarator, semanticModel, cancellationToken, visited);
                            }

                            if (DisposableWalker.Ignores(candidate, semanticModel, cancellationToken, visited))
                            {
                                return true;
                            }

                            return false;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Ignores(VariableDeclaratorSyntax declarator, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (declarator.TryFirstAncestor(out BlockSyntax block) &&
                semanticModel.TryGetSymbol(declarator, cancellationToken, out ILocalSymbol local))
            {
                if (declarator.TryFirstAncestor<UsingStatementSyntax>(out _))
                {
                    return false;
                }

                using (var invocations = InvocationWalker.Borrow(block))
                {
                    // Just checking if there is a dispose call in the scope for now.
                    if (invocations.Invocations.TryFirst(x => DisposeCall.IsIDisposableDispose(x, semanticModel, cancellationToken), out _))
                    {
                        return false;
                    }
                }

                using (var walker = IdentifierNameWalker.Borrow(block))
                {
                    walker.RemoveAll(x => !IsMatch(x));
                    if (walker.IdentifierNames.Count == 0)
                    {
                        return true;
                    }

                    return walker.IdentifierNames.All(x => Ignores(x, semanticModel, cancellationToken, visited));
                }

                bool IsMatch(IdentifierNameSyntax candidate)
                {
                    if (candidate.Identifier.Text != local.Name)
                    {
                        return false;
                    }

                    return semanticModel.TryGetSymbol(candidate, cancellationToken, out ILocalSymbol other) &&
                           other.Equals(local);
                }
            }

            return false;
        }

        [Obsolete("Use DisposableWalker")]
        private static Result IsChainedDisposingInReturnValue(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
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
                        return !Disposable.IsAssignableFrom(type, semanticModel.Compilation)
                            ? Result.No
                            : Result.AssumeYes;
                    }

                    return !Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation)
                        ? Result.No
                        : Result.AssumeYes;
                }

                if (method.IsExtensionMethod &&
                    method.ReducedFrom is IMethodSymbol reducedFrom &&
                    reducedFrom.Parameters.TryFirst(out var parameter))
                {
                    return DisposedByReturnValue(parameter, semanticModel, cancellationToken, visited) ? Result.Yes : Result.No;
                }
            }

            return Result.AssumeNo;
        }

        [Obsolete("Use DisposableWalker")]
        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty member)
        {
            member = default(FieldOrProperty);
            if (method == null)
            {
                return false;
            }

            if (method.TryFindParameter(argument, out var parameter))
            {
                if (method.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax methodDeclaration))
                {
                    if (AssignmentExecutionWalker.FirstWith(parameter.OriginalDefinition, (SyntaxNode)methodDeclaration.Body ?? methodDeclaration.ExpressionBody, Scope.Member, semanticModel, cancellationToken, out var assignment))
                    {
                        return semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol symbol) &&
                               FieldOrProperty.TryCreate(symbol, out member);
                    }

                    if (methodDeclaration is ConstructorDeclarationSyntax ctor &&
                        ctor.Initializer is ConstructorInitializerSyntax initializer &&
                        initializer.ArgumentList != null &&
                        initializer.ArgumentList.Arguments.TrySingle(x => x.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == parameter.Name, out var chainedArgument) &&
                        semanticModel.TryGetSymbol(initializer, cancellationToken, out var chained))
                    {
                        return TryGetAssignedFieldOrProperty(chainedArgument, chained, semanticModel, cancellationToken, out member);
                    }
                }
                else if (method == KnownSymbol.Tuple.Create)
                {
                    return method.ReturnType.TryFindProperty(parameter.Name.ToFirstCharUpper(), out var field) &&
                           FieldOrProperty.TryCreate(field, out member);
                }
                else if (method.MethodKind == MethodKind.Constructor &&
                         method.ContainingType.MetadataName.StartsWith("Tuple`"))
                {
                    return method.ContainingType.TryFindProperty(parameter.Name.ToFirstCharUpper(), out var field) &&
                           FieldOrProperty.TryCreate(field, out member);
                }
            }

            return false;
        }
    }
}
