namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ISymbol? container)
        {
            using var recursion = Recursion.Borrow(localOrParameter.Symbol.ContainingType, semanticModel, cancellationToken);
            using var walker = UsagesWalker.Borrow(localOrParameter, semanticModel, cancellationToken);
            foreach (var usage in walker.Usages)
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
            if (target.Declaration is { })
            {
                using var walker = UsagesWalker.Borrow(target.Symbol, target.Declaration, recursion.SemanticModel, recursion.CancellationToken);
                foreach (var usage in walker.Usages)
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
            switch (candidate)
            {
                case { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax { ArgumentList: { Arguments: { Count: 1 } arguments } } invocation } }
                    when invocation.IsSymbol(KnownSymbol.DisposableMixins.DisposeWith, recursion.SemanticModel, recursion.CancellationToken) &&
                         recursion.SemanticModel.TryGetSymbol(arguments[0].Expression, recursion.CancellationToken, out container):
                    return true;
                case { Parent: InitializerExpressionSyntax { Parent: ImplicitArrayCreationExpressionSyntax arrayCreation } }:
                    return StoresOrAssigns(arrayCreation, out container);
                case { Parent: InitializerExpressionSyntax { Parent: ArrayCreationExpressionSyntax arrayCreation } }:
                    return StoresOrAssigns(arrayCreation, out container);
                case { Parent: InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax objectInitializer } }:
                    return StoresOrAssigns(objectInitializer, out container);
                case { Parent: AssignmentExpressionSyntax { Right: { } right, Left: ElementAccessExpressionSyntax { Expression: { } element } } }
                    when right.Contains(candidate):
                    return recursion.SemanticModel.TryGetSymbol(element, recursion.CancellationToken, out container);
                case { }
                    when Identity(candidate, recursion) is { } id &&
                         Stores(id, recursion, out container):
                    return true;
                case { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax _ } } argument }
                    when recursion.Target(argument) is { } target:
                    if (DisposedByReturnValue(target, recursion, out var objectCreation) ||
                        AccessibleInReturnValue(target, recursion, out objectCreation))
                    {
                        return StoresOrAssigns(objectCreation, out container);
                    }

                    container = null;
                    return false;
                case { Parent: ArgumentSyntax { Parent: TupleExpressionSyntax tupleExpression } }:
                    return StoresOrAssigns(tupleExpression, out container);
                case { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument }
                    when recursion.Target(argument) is { Symbol: { } parameter } target:
                    if (target.Declaration is null &&
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

                case { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator } }
                    when recursion.Target(variableDeclarator) is { } target:
                    return Stores(target, recursion, out container);
                default:
                    container = null;
                    return false;
            }

            bool StoresOrAssigns(ExpressionSyntax expression, out ISymbol? result)
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
                        invocation.Parent.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        return false;
                    }

                    if (target.Declaration is { })
                    {
                        using var walker = UsagesWalker.Borrow(target.Symbol, target.Declaration, recursion.SemanticModel, recursion.CancellationToken);
                        foreach (var usage in walker.Usages)
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

                case { Symbol: { ContainingSymbol: { } constructor }, Source: { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax objectCreation } } }:
                    if (constructor.ContainingType.MetadataName.StartsWith("Tuple`", StringComparison.Ordinal))
                    {
                        creation = objectCreation;
                        return true;
                    }

                    if (target.Declaration is { })
                    {
                        using var walker = UsagesWalker.Borrow(target.Symbol, target.Declaration, recursion.SemanticModel, recursion.CancellationToken);
                        foreach (var usage in walker.Usages)
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
