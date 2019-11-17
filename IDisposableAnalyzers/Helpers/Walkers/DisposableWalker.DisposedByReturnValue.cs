namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool DisposedByReturnValue(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ExpressionSyntax? invocationOrObjectCreation)
        {
            switch (candidate)
            {
                case { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax { Type: { } type } objectCreation } }:
                    if (type == KnownSymbol.SingleAssignmentDisposable ||
                        type == KnownSymbol.RxDisposable ||
                        type == KnownSymbol.CompositeDisposable)
                    {
                        invocationOrObjectCreation = objectCreation;
                        return true;
                    }

                    if (semanticModel.TryGetSymbol(objectCreation, cancellationToken, out var constructor))
                    {
                        if (Disposable.IsAssignableFrom(constructor.ContainingType, semanticModel.Compilation))
                        {
                            if (constructor.ContainingType == KnownSymbol.BinaryReader ||
                                constructor.ContainingType == KnownSymbol.BinaryWriter ||
                                constructor.ContainingType == KnownSymbol.StreamReader ||
                                constructor.ContainingType == KnownSymbol.StreamWriter ||
                                constructor.ContainingType == KnownSymbol.CryptoStream ||
                                constructor.ContainingType == KnownSymbol.DeflateStream ||
                                constructor.ContainingType == KnownSymbol.GZipStream ||
                                constructor.ContainingType == KnownSymbol.StreamMemoryBlockProvider)
                            {
                                if (constructor.TryFindParameter("leaveOpen", out var leaveOpenParameter) &&
                                    objectCreation.TryFindArgument(leaveOpenParameter, out var leaveOpenArgument) &&
                                    leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                    literal.IsKind(SyntaxKind.TrueLiteralExpression))
                                {
                                    invocationOrObjectCreation = null;
                                    return false;
                                }

                                invocationOrObjectCreation = objectCreation;
                                return true;
                            }

                            if (constructor.TryFindParameter(candidate, out var parameter))
                            {
                                if (parameter.Type.IsAssignableTo(KnownSymbol.HttpMessageHandler, semanticModel.Compilation) &&
                                    constructor.ContainingType.IsAssignableTo(KnownSymbol.HttpClient, semanticModel.Compilation))
                                {
                                    if (constructor.TryFindParameter("disposeHandler", out var leaveOpenParameter) &&
                                        objectCreation.TryFindArgument(leaveOpenParameter, out var leaveOpenArgument) &&
                                        leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                        literal.IsKind(SyntaxKind.FalseLiteralExpression))
                                    {
                                        invocationOrObjectCreation = null;
                                        return false;
                                    }

                                    invocationOrObjectCreation = objectCreation;
                                    return true;
                                }

                                if (Target(candidate, semanticModel, cancellationToken, visited) is { } target &&
                                    DisposedByReturnValue(target, semanticModel, cancellationToken, visited))
                                {
                                    invocationOrObjectCreation = objectCreation;
                                    return true;
                                }
                            }
                        }
                    }

                    break;
                case { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }:
                    if (semanticModel.TryGetSymbol(invocation, cancellationToken, out IMethodSymbol? method))
                    {
                        if (method == KnownSymbol.Task.FromResult)
                        {
                            invocationOrObjectCreation = invocation;
                            return true;
                        }

                        if (Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation) &&
                            Target(candidate, semanticModel, cancellationToken, visited) is { } ctorTarget &&
                            DisposedByReturnValue(ctorTarget, semanticModel, cancellationToken, visited))
                        {
                            invocationOrObjectCreation = invocation;
                            return true;
                        }
                    }

                    break;
            }

            invocationOrObjectCreation = null;
            return false;
        }

        private static bool DisposedByReturnValue(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ExpressionSyntax? invocationOrObjectCreation)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return DisposedByReturnValue((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited, out invocationOrObjectCreation);
            }

            switch (candidate.Parent)
            {
                case ArgumentSyntax argument:
                    return DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out invocationOrObjectCreation);
                case InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax objectCreation }
                    when semanticModel.TryGetType(objectCreation, cancellationToken, out var type) &&
                         type == KnownSymbol.CompositeDisposable:
                    invocationOrObjectCreation = objectCreation;
                    return true;
                default:
                    invocationOrObjectCreation = null;
                    return false;
            }
        }

        internal static bool DisposedByReturnValue(Target<ArgumentSyntax, IParameterSymbol, BaseMethodDeclarationSyntax> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, [NotNullWhen(true)] out ExpressionSyntax? invocationOrObjectCreation)
        {
            switch (target)
            {
                case { Symbol: { ContainingSymbol: IMethodSymbol constructor } parameter, Source: { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax { Type: { } type } objectCreation } } }:
                    if (type == KnownSymbol.SingleAssignmentDisposable ||
                        type == KnownSymbol.RxDisposable ||
                        type == KnownSymbol.CompositeDisposable)
                    {
                        invocationOrObjectCreation = objectCreation;
                        return true;
                    }

                    if (Disposable.IsAssignableFrom(target.Symbol.ContainingType, semanticModel.Compilation))
                    {
                        if (constructor.ContainingType == KnownSymbol.BinaryReader ||
                            constructor.ContainingType == KnownSymbol.BinaryWriter ||
                            constructor.ContainingType == KnownSymbol.StreamReader ||
                            constructor.ContainingType == KnownSymbol.StreamWriter ||
                            constructor.ContainingType == KnownSymbol.CryptoStream ||
                            constructor.ContainingType == KnownSymbol.DeflateStream ||
                            constructor.ContainingType == KnownSymbol.GZipStream ||
                            constructor.ContainingType == KnownSymbol.StreamMemoryBlockProvider)
                        {
                            if (constructor.TryFindParameter("leaveOpen", out var leaveOpenParameter) &&
                                objectCreation.TryFindArgument(leaveOpenParameter, out var leaveOpenArgument) &&
                                leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                literal.IsKind(SyntaxKind.TrueLiteralExpression))
                            {
                                invocationOrObjectCreation = null;
                                return false;
                            }

                            invocationOrObjectCreation = objectCreation;
                            return true;
                        }

                        if (parameter.Type.IsAssignableTo(KnownSymbol.HttpMessageHandler, semanticModel.Compilation) &&
                            constructor.ContainingType.IsAssignableTo(KnownSymbol.HttpClient, semanticModel.Compilation))
                        {
                            if (constructor.TryFindParameter("disposeHandler", out var leaveOpenParameter) &&
                                objectCreation.TryFindArgument(leaveOpenParameter, out var leaveOpenArgument) &&
                                leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                literal.IsKind(SyntaxKind.FalseLiteralExpression))
                            {
                                invocationOrObjectCreation = null;
                                return false;
                            }

                            invocationOrObjectCreation = objectCreation;
                            return true;
                        }

                        if (DisposedByReturnValue(target, semanticModel, cancellationToken, visited))
                        {
                            invocationOrObjectCreation = objectCreation;
                            return true;
                        }
                    }

                    break;
                case { Symbol: { ContainingSymbol: IMethodSymbol method } parameter, Source: { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } }:
                    if (method == KnownSymbol.Task.FromResult)
                    {
                        invocationOrObjectCreation = invocation;
                        return true;
                    }

                    if (Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation) &&
                        DisposedByReturnValue(target, semanticModel, cancellationToken, visited))
                    {
                        invocationOrObjectCreation = invocation;
                        return true;
                    }

                    break;
            }

            invocationOrObjectCreation = null;
            return false;
        }

        private static bool DisposedByReturnValue<TSource>(Target<TSource, IParameterSymbol, BaseMethodDeclarationSyntax> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
            where TSource : SyntaxNode
        {
            using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
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
                        DisposableMember.IsDisposed(fieldOrProperty, target.Symbol.ContainingType, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes))
                    {
                        return true;
                    }

                    if (usage.Parent is ArgumentSyntax argument &&
                        DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out var invocationOrObjectCreation) &&
                        Returns(invocationOrObjectCreation, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
