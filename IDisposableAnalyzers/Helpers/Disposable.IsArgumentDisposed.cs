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
        internal static Result IsDisposedByReturnValue(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
        {
            if (argument?.Parent is ArgumentListSyntax argumentList)
            {
                if (argumentList.Parent is InvocationExpressionSyntax invocation &&
                     semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method)
                {
                    if (method.ReturnsVoid)
                    {
                        return Result.No;
                    }

                    if (!method.TrySingleMethodDeclaration(cancellationToken, out _))
                    {
                        return IsAssignableFrom(method.ReturnType, semanticModel.Compilation)
                            ? Result.AssumeYes
                            : Result.No;
                    }

                    if (method.TryFindParameter(argument, out var parameter))
                    {
                        return CheckReturnValues(parameter, invocation, semanticModel, cancellationToken, visited);
                    }

                    return Result.Unknown;
                }

                if (argumentList.Parent is ObjectCreationExpressionSyntax ||
                    argumentList.Parent is ConstructorInitializerSyntax)
                {
                    if (TryGetAssignedFieldOrProperty(argument, semanticModel, cancellationToken, out var fieldOrProperty, out var ctor))
                    {
                        var initializer = argument.FirstAncestorOrSelf<ConstructorInitializerSyntax>();
                        if (initializer != null)
                        {
                            if (semanticModel.GetDeclaredSymbolSafe(initializer.Parent, cancellationToken) is IMethodSymbol chainedCtor &&
                                chainedCtor.ContainingType != fieldOrProperty.ContainingType)
                            {
                                if (DisposeMethod.TryFindFirst(chainedCtor.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod))
                                {
                                    return DisposableMember.IsDisposed(fieldOrProperty, disposeMethod, semanticModel, cancellationToken)
                                        ? Result.Yes
                                        : Result.No;
                                }
                            }
                        }

                        return DisposableMember.IsDisposed(fieldOrProperty, ctor.ContainingType, semanticModel, cancellationToken);
                    }

                    if (ctor == null)
                    {
                        return Result.AssumeYes;
                    }

                    if (ctor.ContainingType.DeclaringSyntaxReferences.Length == 0)
                    {
                        return IsAssignableFrom(ctor.ContainingType, semanticModel.Compilation) ? Result.AssumeYes : Result.No;
                    }

                    if (ctor.ContainingType.IsAssignableTo(KnownSymbol.NinjectStandardKernel, semanticModel.Compilation))
                    {
                        return Result.Yes;
                    }

                    return Result.No;
                }
            }

            return Result.Unknown;
        }

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
                                foreach (var nested in walker)
                                {
                                    if (nested.ArgumentList != null &&
                                        nested.ArgumentList.Arguments.TryFirst(x => x.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == parameter.Name, out var nestedArg))
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

        private static Result CheckReturnValues(IParameterSymbol parameter, SyntaxNode memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            var result = Result.No;
            using (var returnWalker = ReturnValueWalker.Borrow(memberAccess, ReturnValueSearch.Recursive, semanticModel, cancellationToken))
            {
#pragma warning disable IDISP003
                using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                {
                    if (!visited.Add(memberAccess))
                    {
                        return Result.Unknown;
                    }

                    foreach (var returnValue in returnWalker)
                    {
                        switch (CheckReturnValue(returnValue))
                        {
                            case Result.Unknown:
                                return Result.Unknown;
                            case Result.Yes:
                                if (result == Result.No)
                                {
                                    result = Result.Yes;
                                }

                                break;
                            case Result.AssumeYes:
                                result = Result.AssumeYes;
                                break;
                            case Result.No:
                                return Result.No;
                            case Result.AssumeNo:
                                return Result.AssumeNo;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            return result;

            Result CheckReturnValue(ExpressionSyntax returnValue)
            {
                if (returnValue is ObjectCreationExpressionSyntax nestedObjectCreation)
                {
                    if (nestedObjectCreation.TryFindArgument(parameter, out var nestedArgument))
                    {
                        return IsDisposedByReturnValue(nestedArgument, semanticModel, cancellationToken, visited);
                    }

                    return Result.No;
                }

                if (returnValue is InvocationExpressionSyntax nestedInvocation)
                {
                    if (nestedInvocation.TryFindArgument(parameter, out var nestedArgument))
                    {
                        return IsDisposedByReturnValue(nestedArgument, semanticModel, cancellationToken, visited);
                    }

                    return Result.No;
                }

                if (returnValue is MemberAccessExpressionSyntax nestedMemberAccess)
                {
                    return IsChainedDisposingInReturnValue(nestedMemberAccess, semanticModel, cancellationToken, visited);
                }

                if (returnValue is ConditionalAccessExpressionSyntax conditionalAccess)
                {
                    return IsChainedDisposingInReturnValue(conditionalAccess, semanticModel, cancellationToken, visited);
                }

                return Result.Unknown;
            }
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

            if (TryFindParameter(out var parameter))
            {
                if (method.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax methodDeclaration))
                {
                    if (AssignmentExecutionWalker.FirstWith(parameter, methodDeclaration.Body, Scope.Member, semanticModel, cancellationToken, out var assignment))
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

            // https://github.com/GuOrg/Gu.Roslyn.Extensions/issues/40
            bool TryFindParameter(out IParameterSymbol result)
            {
                return method.TryFindParameter(argument, out result) ||
                       (method.Parameters.TryLast(out result) &&
                        result.IsParams);
            }
        }
    }
}
