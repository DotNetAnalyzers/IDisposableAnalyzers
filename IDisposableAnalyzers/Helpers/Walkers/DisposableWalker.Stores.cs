namespace IDisposableAnalyzers
{
    using System.Runtime.InteropServices.ComTypes;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        public static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out ISymbol container)
        {
            using (var walker = CreateUsagesWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Stores(usage, semanticModel, cancellationToken, visited, out container))
                    {
                        return true;
                    }
                }
            }

            container = null;
            return false;
        }

        public static bool DisposedByReturnValue(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (candidate.Parent is ArgumentListSyntax argumentList &&
                semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method))
            {
                if (method.MethodKind == MethodKind.Constructor)
                {
                    if (method.ContainingType == KnownSymbol.SingleAssignmentDisposable ||
                        method.ContainingType == KnownSymbol.RxDisposable ||
                        method.ContainingType == KnownSymbol.CompositeDisposable)
                    {
                        return true;
                    }

                    if (Disposable.IsAssignableFrom(method.ContainingType, semanticModel.Compilation))
                    {
                        if (method.ContainingType == KnownSymbol.BinaryReader ||
                            method.ContainingType == KnownSymbol.BinaryWriter ||
                            method.ContainingType == KnownSymbol.StreamReader ||
                            method.ContainingType == KnownSymbol.StreamWriter ||
                            method.ContainingType == KnownSymbol.CryptoStream ||
                            method.ContainingType == KnownSymbol.DeflateStream ||
                            method.ContainingType == KnownSymbol.GZipStream ||
                            method.ContainingType == KnownSymbol.StreamMemoryBlockProvider)
                        {
                            if (method.TryFindParameter("leaveOpen", out var leaveOpenParameter) &&
                                argumentList.TryFind(leaveOpenParameter, out var leaveOpenArgument) &&
                                leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                literal.IsKind(SyntaxKind.TrueLiteralExpression))
                            {
                                return false;
                            }

                            return true;
                        }

                        if (method.TryFindParameter(candidate, out var parameter))
                        {
                            return DisposedByReturnValue(parameter, semanticModel, cancellationToken, visited);
                        }
                    }
                }
                else if (method.MethodKind == MethodKind.Ordinary &&
                         Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation) &&
                         method.TryFindParameter(candidate, out var parameter))
                {
                    return DisposedByReturnValue(parameter, semanticModel, cancellationToken, visited);
                }
            }

            return false;
        }

        public static bool DisposedByReturnValue(IParameterSymbol candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (candidate.TrySingleDeclaration(cancellationToken, out var parameterSyntax) &&
                candidate.ContainingSymbol is IMethodSymbol method)
            {
                if (visited.CanVisit(parameterSyntax, out visited))
                {
                    using (visited)
                    {
                        using (var walker = CreateUsagesWalker(new LocalOrParameter(candidate), semanticModel, cancellationToken))
                        {
                            foreach (var usage in walker.usages)
                            {
                                switch (usage.Parent.Kind())
                                {
                                    case SyntaxKind.ReturnStatement:
                                    case SyntaxKind.ArrowExpressionClause:
                                        return true;
                                }

                                if (Assigns(usage, semanticModel, cancellationToken, visited, out var fieldOrProperty) &&
                                    DisposableMember.IsDisposed(fieldOrProperty, method.ContainingType, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes))
                                {
                                    return true;
                                }

                                if (usage.Parent is ArgumentSyntax argument &&
                                    argument.Parent is ArgumentListSyntax argumentList &&
                                    DisposedByReturnValue(argument, semanticModel, cancellationToken, visited) &&
                                    argumentList.Parent is ExpressionSyntax parentExpression &&
                                    Returns(parentExpression, semanticModel, cancellationToken, visited))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool Stores(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out ISymbol container)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.ArrayInitializerExpression:
                case SyntaxKind.CollectionInitializerExpression:
                    return StoresOrAssigns((ExpressionSyntax)candidate.Parent.Parent, out container);
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return Stores((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited, out container);
            }

            switch (candidate.Parent)
            {
                case AssignmentExpressionSyntax assignment when assignment.Right.Contains(candidate) &&
                                                                assignment.Left is ElementAccessExpressionSyntax elementAccess:
                    return semanticModel.TryGetSymbol(elementAccess.Expression, cancellationToken, out container);
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is ExpressionSyntax invocationOrObjectCreation &&
                                                  (DisposedByReturnValue(argument, semanticModel, cancellationToken, visited) ||
                                                   ReturnedInAccessible(argument, semanticModel, cancellationToken, visited)):
                    return StoresOrAssigns(invocationOrObjectCreation, out container);
                case ArgumentSyntax argument when argument.Parent is TupleExpressionSyntax tupleExpression:
                    return Stores(tupleExpression, semanticModel, cancellationToken, visited, out container) ||
                           Assigns(tupleExpression, semanticModel, cancellationToken, visited, out _);
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                  semanticModel.TryGetSymbol(invocation, cancellationToken, out var method):
                    {
                        if (method.DeclaringSyntaxReferences.IsEmpty)
                        {
                            if (method.ContainingType.AllInterfaces.TryFirst(x => x == KnownSymbol.IEnumerable, out _) &&
                                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                            {
                                switch (method.Name)
                                {
                                    case "Add":
                                    case "Insert":
                                    case "Push":
                                    case "Enqueue":
                                    case "GetOrAdd":
                                    case "AddOrUpdate":
                                    case "TryAdd":
                                    case "TryUpdate":
                                        _ = semanticModel.TryGetSymbol(memberAccess.Expression, cancellationToken, out container);
                                        return true;
                                }
                            }

                            container = null;
                            return false;
                        }

                        if (method.TryFindParameter(argument, out var parameter) &&
                            visited.CanVisit(candidate, out visited))
                        {
                            using (visited)
                            {
                                if (Stores(new LocalOrParameter(parameter), semanticModel, cancellationToken, visited, out container))
                                {
                                    return true;
                                }
                            }

                            container = null;
                            return false;
                        }

                        container = null;
                        return false;
                    }

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out container) &&
                                                                    LocalOrParameter.TryCreate(container, out var local):
                    if (visited.CanVisit(candidate, out visited))
                    {
                        using (visited)
                        {
                            return Stores(local, semanticModel, cancellationToken, visited, out container);
                        }
                    }

                    container = null;
                    return false;
                default:
                    container = null;
                    return false;
            }

            bool StoresOrAssigns(ExpressionSyntax expression, out ISymbol result)
            {
                if (Stores(expression, semanticModel, cancellationToken, visited, out result))
                {
                    return true;
                }

                if (Assigns(expression, semanticModel, cancellationToken, visited, out var fieldOrProperty))
                {
                    result = fieldOrProperty.Symbol;
                    return true;
                }

                result = null;
                return false;
            }
        }

        private static bool ReturnedInAccessible(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (candidate.Parent is ArgumentListSyntax argumentList)
            {
                switch (argumentList.Parent)
                {
                    case InvocationExpressionSyntax invocation when semanticModel.TryGetSymbol(invocation, cancellationToken, out var method):
                        {
                            if (method.DeclaringSyntaxReferences.IsEmpty)
                            {
                                return method == KnownSymbol.Tuple.Create;
                            }

                            if (method.ReturnsVoid ||
                                invocation.Parent.Kind() == SyntaxKind.ExpressionStatement)
                            {
                                return false;
                            }

                            if (method.TryFindParameter(candidate, out var parameter) &&
                                visited.CanVisit(candidate, out visited))
                            {
                                using (visited)
                                {
                                    using (var walker = CreateUsagesWalker(new LocalOrParameter(parameter), semanticModel, cancellationToken))
                                    {
                                        foreach (var usage in walker.usages)
                                        {
                                            if (usage.Parent is ArgumentSyntax parentArgument &&
                                                parentArgument.Parent is ArgumentListSyntax parentArgumentList &&
                                                parentArgumentList.Parent is ExpressionSyntax parentInvocationOrObjectCreation &&
                                                ReturnedInAccessible(parentArgument, semanticModel, cancellationToken, visited) &&
                                                Returns(parentInvocationOrObjectCreation, semanticModel, cancellationToken, visited))
                                            {
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }

                            return false;
                        }

                    case ObjectCreationExpressionSyntax objectCreation when semanticModel.TryGetSymbol(objectCreation, cancellationToken, out var method):
                        {
                            if (method.DeclaringSyntaxReferences.IsEmpty)
                            {
                                return method.ContainingType.FullName().StartsWith("System.Tuple`");
                            }

                            if (method.TryFindParameter(candidate, out var parameter))
                            {
                                if (Stores(new LocalOrParameter(parameter), semanticModel, cancellationToken, visited, out var container))
                                {
                                    return FieldOrProperty.TryCreate(container, out var containerMember) &&
                                           semanticModel.IsAccessible(candidate.SpanStart, containerMember.Symbol);
                                }

                                if (Assigns(new LocalOrParameter(parameter), semanticModel, cancellationToken, visited, out var fieldOrProperty))
                                {
                                    return semanticModel.IsAccessible(candidate.SpanStart, fieldOrProperty.Symbol);
                                }
                            }

                            return false;
                        }
                }
            }

            return false;
        }
    }
}
