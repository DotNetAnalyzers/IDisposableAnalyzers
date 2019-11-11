namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)> visited, out ISymbol container)
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

        private static bool Stores(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)> visited, out ISymbol container)
        {
            switch (candidate.Parent)
            {
                case CastExpressionSyntax cast:
                    return StoresOrAssigns(cast, out container);
                case InitializerExpressionSyntax { Parent: ImplicitArrayCreationExpressionSyntax arrayCreation }:
                    return StoresOrAssigns(arrayCreation, out container);
                case InitializerExpressionSyntax { Parent: ArrayCreationExpressionSyntax arrayCreation }:
                    return StoresOrAssigns(arrayCreation, out container);
                case InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax objectInitializer }:
                    return StoresOrAssigns(objectInitializer, out container);
                case AssignmentExpressionSyntax { Right: { } right, Left: ElementAccessExpressionSyntax { Expression: { } element } }
                    when right.Contains(candidate):
                    return semanticModel.TryGetSymbol(element, cancellationToken, out container);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax _ } } argument:
                    if (DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out var objectCreation) ||
                        AccessibleInReturnValue(argument, semanticModel, cancellationToken, visited, out objectCreation))
                    {
                        return StoresOrAssigns(objectCreation, out container);
                    }

                    container = null;
                    return false;
                case ArgumentSyntax { Parent: TupleExpressionSyntax tupleExpression }:
                    return Stores(tupleExpression, semanticModel, cancellationToken, visited, out container) ||
                           Assigns(tupleExpression, semanticModel, cancellationToken, visited, out _);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
                    when semanticModel.TryGetSymbol(invocation, cancellationToken, out var method):
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
                        }
                        else if (method.TryFindParameter(argument, out var parameter) &&
                                 visited.CanVisit(candidate, out visited))
                        {
                            using (visited)
                            {
                                if (Stores(new LocalOrParameter(parameter), semanticModel, cancellationToken, visited, out container))
                                {
                                    return true;
                                }
                            }
                        }

                        if (DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out var invocationOrObjectCreation) ||
                            AccessibleInReturnValue(argument, semanticModel, cancellationToken, visited, out invocationOrObjectCreation))
                        {
                            return StoresOrAssigns(invocationOrObjectCreation, out container);
                        }

                        container = null;
                        return false;
                    }

                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out container) &&
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
                case ExpressionSyntax parent
                    when parent.IsKind(SyntaxKind.AsExpression) ||
                         parent.IsKind(SyntaxKind.ConditionalExpression) ||
                         parent.IsKind(SyntaxKind.CoalesceExpression):
                    return Stores(parent, semanticModel, cancellationToken, visited, out container);
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

        private static bool AccessibleInReturnValue(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)> visited, out ExpressionSyntax invocationOrObjectCreation)
        {
            switch (candidate)
            {
                case { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }
                    when semanticModel.TryGetSymbol(invocation, cancellationToken, out var method):
                    invocationOrObjectCreation = invocation;
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
                                        AccessibleInReturnValue(parentArgument, semanticModel, cancellationToken, visited, out var parentInvocationOrObjectCreation) &&
                                        Returns(parentInvocationOrObjectCreation, semanticModel, cancellationToken, visited))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }

                    return false;

                case { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax objectCreation } }
                    when semanticModel.TryGetSymbol(objectCreation, cancellationToken, out var constructor):
                    invocationOrObjectCreation = objectCreation;
                    if (constructor.DeclaringSyntaxReferences.IsEmpty)
                    {
                        return constructor.ContainingType.FullName().StartsWith("System.Tuple`", StringComparison.Ordinal);
                    }

                    if (constructor.TryFindParameter(candidate, out parameter))
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
                default:
                    invocationOrObjectCreation = null;
                    return false;
            }
        }
    }
}
