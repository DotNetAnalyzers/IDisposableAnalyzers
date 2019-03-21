namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        private static Result IsAssignedToDisposable(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
        {
            if (argument?.Parent is ArgumentListSyntax argumentList)
            {
                if (semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method))
                {
                    if (method == KnownSymbol.CompositeDisposable.Add)
                    {
                        return Result.Yes;
                    }

                    if (TryGetAssignedFieldOrProperty(argument, method, semanticModel, cancellationToken, out _))
                    {
                        return Result.Yes;
                    }

                    if (method.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax declaration) &&
                        method.TryFindParameter(argument, out var parameter))
                    {
                        using (visited = visited.IncrementUsage())
                        {
                            using (var walker = InvocationWalker.Borrow(declaration))
                            {
                                foreach (var nested in walker.Invocations)
                                {
                                    if (nested.TryFindArgument(parameter, out var nestedArg) &&
                                        visited.Add(nestedArg))
                                    {
                                        switch (IsAssignedToDisposable(nestedArg, semanticModel, cancellationToken, visited))
                                        {
                                            case Result.Unknown:
                                                break;
                                            case Result.Yes:
                                                return Result.Yes;
                                            case Result.AssumeYes:
                                                return Result.AssumeYes;
                                            case Result.No:
                                                break;
                                            case Result.AssumeNo:
                                                break;
                                            default:
                                                throw new ArgumentOutOfRangeException();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return Result.No;
                }
            }

            return Result.No;
        }

        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty member, out IMethodSymbol ctor)
        {
            if (TryGetConstructor(argument, semanticModel, cancellationToken, out ctor))
            {
                return TryGetAssignedFieldOrProperty(argument, ctor, semanticModel, cancellationToken, out member);
            }

            member = default(FieldOrProperty);
            return false;
        }

        private static bool TryGetConstructor(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol ctor)
        {
            ctor = null;
            if (argument.Parent is ArgumentListSyntax argumentList)
            {
                switch (argumentList.Parent)
                {
                    case ObjectCreationExpressionSyntax objectCreation:
                        return semanticModel.TryGetSymbol(objectCreation, cancellationToken, out ctor);
                    case ConstructorInitializerSyntax initializer:
                        return semanticModel.TryGetSymbol(initializer, cancellationToken, out ctor);
                }
            }

            return false;
        }

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
