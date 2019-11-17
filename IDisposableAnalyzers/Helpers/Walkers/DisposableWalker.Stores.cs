namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ISymbol? container)
        {
            using (var walker = CreateUsagesWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Stores(usage, semanticModel, cancellationToken, visited.IncrementUsage(), out container))
                    {
                        return true;
                    }
                }
            }

            container = null;
            return false;
        }

        private static bool Stores<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ISymbol? container)
            where TSource : SyntaxNode
            where TSymbol : ISymbol
            where TNode : SyntaxNode
        {
            if (target.TargetNode is { })
            {
                using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (Stores(usage, semanticModel, cancellationToken, visited, out container))
                        {
                            return true;
                        }
                    }
                }
            }

            container = null;
            return false;
        }

        private static bool Stores(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ISymbol? container)
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
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax _ } } argument
                    when Target(argument, semanticModel, cancellationToken, visited) is { } target:
                    if (DisposedByReturnValue(target, semanticModel, cancellationToken, visited, out var objectCreation) ||
                        AccessibleInReturnValue(target, semanticModel, cancellationToken, visited, out objectCreation))
                    {
                        return StoresOrAssigns(objectCreation, out container);
                    }

                    container = null;
                    return false;
                case ArgumentSyntax { Parent: TupleExpressionSyntax tupleExpression }:
                    return StoresOrAssigns(tupleExpression, out container);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
                    when Target(argument, semanticModel, cancellationToken, visited) is { Symbol: { } parameter } target:
                    if (target.TargetNode is null &&
                        parameter.ContainingType.AllInterfaces.TryFirst(x => x == KnownSymbol.IEnumerable, out _) &&
                        invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        switch (parameter.ContainingSymbol.Name)
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

                    if (Stores(target, semanticModel, cancellationToken, visited, out container))
                    {
                        return true;
                    }

                    if (DisposedByReturnValue(target, semanticModel, cancellationToken, visited, out var creation) ||
                        AccessibleInReturnValue(target, semanticModel, cancellationToken, visited, out creation))
                    {
                        return StoresOrAssigns(creation, out container);
                    }

                    container = null;
                    return false;

                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when Target(variableDeclarator, semanticModel, cancellationToken, visited) is { } target:
                    return Stores(target, semanticModel, cancellationToken, visited, out container);

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

                result = null!;
                return false;
            }
        }

        [Obsolete("Use target")]
        private static bool AccessibleInReturnValue(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ExpressionSyntax? invocationOrObjectCreation)
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
                    if (constructor.ContainingType.MetadataName.StartsWith("Tuple`", StringComparison.Ordinal))
                    {
                        invocationOrObjectCreation = objectCreation;
                        return true;
                    }

                    if (constructor.TryFindParameter(candidate, out parameter))
                    {
                        if (Stores(new LocalOrParameter(parameter), semanticModel, cancellationToken, visited, out var container) &&
                            FieldOrProperty.TryCreate(container, out var containerMember) &&
                            semanticModel.IsAccessible(candidate.SpanStart, containerMember.Symbol))
                        {
                            invocationOrObjectCreation = objectCreation;
                            return true;
                        }

                        if (Assigns(new LocalOrParameter(parameter), semanticModel, cancellationToken, visited, out var fieldOrProperty) &&
                            semanticModel.IsAccessible(candidate.SpanStart, fieldOrProperty.Symbol))
                        {
                            invocationOrObjectCreation = objectCreation;
                            return true;
                        }
                    }

                    invocationOrObjectCreation = default;
                    return false;
                default:
                    invocationOrObjectCreation = null;
                    return false;
            }
        }

        private static bool AccessibleInReturnValue(Target<ArgumentSyntax, IParameterSymbol, BaseMethodDeclarationSyntax> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ExpressionSyntax? invocationOrObjectCreation)
        {
            switch (target)
            {
                case { Symbol: { ContainingSymbol: IMethodSymbol method }, Source: { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } }:
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

                    using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
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

                    return false;

                case { Symbol: { ContainingSymbol: { } constructor } parameter, Source: { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax objectCreation } } }:
                    if (constructor.ContainingType.MetadataName.StartsWith("Tuple`", StringComparison.Ordinal))
                    {
                        invocationOrObjectCreation = objectCreation;
                        return true;
                    }

                    using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
                    {
                        foreach (var usage in walker.usages)
                        {
                            if (Stores(usage, semanticModel, cancellationToken, visited, out var container) &&
                                FieldOrProperty.TryCreate(container, out var containerMember) &&
                                semanticModel.IsAccessible(target.Source.SpanStart, containerMember.Symbol))
                            {
                                invocationOrObjectCreation = objectCreation;
                                return true;
                            }

                            if (Assigns(usage, semanticModel, cancellationToken, visited, out var fieldOrProperty) &&
                                semanticModel.IsAccessible(target.Source.SpanStart, fieldOrProperty.Symbol))
                            {
                                invocationOrObjectCreation = objectCreation;
                                return true;
                            }
                        }
                    }

                    invocationOrObjectCreation = default;
                    return false;
                default:
                    invocationOrObjectCreation = null;
                    return false;
            }
        }
    }
}
