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
        internal static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ISymbol? container)
        {
            using var recursion = Recursion.Borrow(semanticModel, cancellationToken);
            using var walker = CreateUsagesWalker(localOrParameter, recursion);
            foreach (var usage in walker.usages)
            {
                if (Stores(usage, recursion, out container))
                {
                    return true;
                }
            }

            container = null;
            return false;
        }

        private static bool Stores<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion, [NotNullWhen(true)] out ISymbol? container)
            where TSource : SyntaxNode
            where TSymbol : ISymbol
            where TNode : SyntaxNode
        {
            if (target.TargetNode is { })
            {
                using var walker = CreateUsagesWalker(target, recursion);
                foreach (var usage in walker.usages)
                {
                    if (Stores(usage, recursion, out container))
                    {
                        return true;
                    }
                }
            }

            container = null;
            return false;
        }

        private static bool Stores(ExpressionSyntax candidate, Recursion recursion, [NotNullWhen(true)] out ISymbol? container)
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
                    return recursion.SemanticModel.TryGetSymbol(element, recursion.CancellationToken, out container);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax _ } } argument
                    when recursion.Target(argument) is { } target:
                    if (DisposedByReturnValue(target, recursion, out var objectCreation) ||
                        AccessibleInReturnValue(target, recursion, out objectCreation))
                    {
                        return StoresOrAssigns(objectCreation, out container);
                    }

                    container = null;
                    return false;
                case ArgumentSyntax { Parent: TupleExpressionSyntax tupleExpression }:
                    return StoresOrAssigns(tupleExpression, out container);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
                    when recursion.Target(argument) is { Symbol: { } parameter } target:
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
                                _ = recursion.SemanticModel.TryGetSymbol(memberAccess.Expression, recursion.CancellationToken, out container);
                                return true;
                        }
                    }

                    if (Stores(target, recursion, out container))
                    {
                        return true;
                    }

                    if (DisposedByReturnValue(target, recursion, out var creation) ||
                        AccessibleInReturnValue(target, recursion, out creation))
                    {
                        return StoresOrAssigns(creation, out container);
                    }

                    container = null;
                    return false;

                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when recursion.Target(variableDeclarator) is { } target:
                    return Stores(target, recursion, out container);

                case ExpressionSyntax parent
                    when parent.IsKind(SyntaxKind.AsExpression) ||
                         parent.IsKind(SyntaxKind.ConditionalExpression) ||
                         parent.IsKind(SyntaxKind.CoalesceExpression):
                    return Stores(parent, recursion, out container);
                default:
                    container = null;
                    return false;
            }

            bool StoresOrAssigns(ExpressionSyntax expression, out ISymbol result)
            {
                if (Stores(expression, recursion, out result))
                {
                    return true;
                }

                if (Assigns(expression, recursion, out var fieldOrProperty))
                {
                    result = fieldOrProperty.Symbol;
                    return true;
                }

                result = null!;
                return false;
            }
        }

        private static bool AccessibleInReturnValue(Target<ArgumentSyntax, IParameterSymbol, BaseMethodDeclarationSyntax> target, Recursion recursion, [NotNullWhen(true)] out ExpressionSyntax? creation)
        {
            switch (target)
            {
                case { Symbol: { ContainingSymbol: IMethodSymbol method }, Source: { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } }:
                    creation = invocation;
                    if (method.DeclaringSyntaxReferences.IsEmpty)
                    {
                        return method == KnownSymbol.Tuple.Create;
                    }

                    if (method.ReturnsVoid ||
                        invocation.Parent.Kind() == SyntaxKind.ExpressionStatement)
                    {
                        return false;
                    }

                    using (var walker = CreateUsagesWalker(target, recursion))
                    {
                        foreach (var usage in walker.usages)
                        {
                            if (usage.Parent is ArgumentSyntax containingArgument &&
                                recursion.Target(containingArgument) is { } argumentTarget &&
                                AccessibleInReturnValue(argumentTarget, recursion, out var containingCreation) &&
                                Returns(containingCreation, recursion))
                            {
                                return true;
                            }
                        }
                    }

                    return false;

                case { Symbol: { ContainingSymbol: { } constructor } parameter, Source: { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax objectCreation } } }:
                    if (constructor.ContainingType.MetadataName.StartsWith("Tuple`", StringComparison.Ordinal))
                    {
                        creation = objectCreation;
                        return true;
                    }

                    using (var walker = CreateUsagesWalker(target, recursion))
                    {
                        foreach (var usage in walker.usages)
                        {
                            if (Stores(usage, recursion, out var container) &&
                                FieldOrProperty.TryCreate(container, out var containerMember) &&
                                recursion.SemanticModel.IsAccessible(target.Source.SpanStart, containerMember.Symbol))
                            {
                                creation = objectCreation;
                                return true;
                            }

                            if (Assigns(usage, recursion, out var fieldOrProperty) &&
                                recursion.SemanticModel.IsAccessible(target.Source.SpanStart, fieldOrProperty.Symbol))
                            {
                                creation = objectCreation;
                                return true;
                            }
                        }
                    }

                    creation = default;
                    return false;
                default:
                    creation = null;
                    return false;
            }
        }
    }
}
