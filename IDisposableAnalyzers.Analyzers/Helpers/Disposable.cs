namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsPotentiallyAssignableFrom(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                candidate is LiteralExpressionSyntax)
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax objectCreation)
            {
                return semanticModel.TryGetType(objectCreation, cancellationToken, out var type) &&
                       IsAssignableFrom(type, semanticModel.Compilation);
            }
            else
            {
                return semanticModel.TryGetType(candidate, cancellationToken, out var type) &&
                       IsPotentiallyAssignableFrom(type, semanticModel.Compilation);
            }
        }

        internal static bool IsPotentiallyAssignableFrom(ITypeSymbol type, Compilation compilation)
        {
            if (type == null ||
                type is IErrorTypeSymbol)
            {
                return false;
            }

            if (type.IsValueType &&
                !IsAssignableFrom(type, compilation))
            {
                return false;
            }

            if (type.IsSealed &&
                !IsAssignableFrom(type, compilation))
            {
                return false;
            }

            return true;
        }

        internal static bool IsAssignableFrom(ITypeSymbol type, Compilation compilation)
        {
            if (type == null)
            {
                return false;
            }

            // https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
            if (type == KnownSymbol.Task)
            {
                return false;
            }

            return type == KnownSymbol.IDisposable ||
                   type.IsAssignableTo(KnownSymbol.IDisposable, compilation);
        }

        internal static bool TryGetDisposeMethod(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (type == null)
            {
                return false;
            }

            var disposers = type.GetMembers("Dispose");
            switch (disposers.Length)
            {
                case 0:
                    var baseType = type.BaseType;
                    if (search == Search.Recursive &&
                        IsAssignableFrom(baseType, compilation))
                    {
                        return TryGetDisposeMethod(baseType, compilation, Search.Recursive, out disposeMethod);
                    }

                    return false;
                case 1:
                    disposeMethod = disposers[0] as IMethodSymbol;
                    if (disposeMethod == null)
                    {
                        return false;
                    }

                    return (disposeMethod.Parameters.Length == 0 &&
                            disposeMethod.DeclaredAccessibility == Accessibility.Public) ||
                           (disposeMethod.Parameters.Length == 1 &&
                            disposeMethod.Parameters[0].Type == KnownSymbol.Boolean);
                case 2:
                    if (disposers.TrySingle(x => (x as IMethodSymbol)?.Parameters.Length == 1, out ISymbol temp))
                    {
                        disposeMethod = temp as IMethodSymbol;
                        return disposeMethod != null &&
                               disposeMethod.Parameters[0].Type == KnownSymbol.Boolean;
                    }

                    break;
            }

            return false;
        }

        internal static bool TryGetBaseVirtualDisposeMethod(ITypeSymbol type, out IMethodSymbol result)
        {
            return type.TryFindFirstMethodRecursive("Dispose", IsVirtualDispose, out result);

            bool IsVirtualDispose(IMethodSymbol candidate)
            {
                return candidate.IsVirtual &&
                       candidate.ReturnsVoid &&
                       candidate.Parameters.TrySingle(out var parameter) &&
                       parameter.Type == KnownSymbol.Boolean;
            }
        }

        internal static bool IsDisposedAfter(ISymbol local, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location.FirstAncestorOrSelf<MemberDeclarationSyntax>() is MemberDeclarationSyntax scope)
            {
                using (var walker = InvocationWalker.Borrow(scope))
                {
                    foreach (var invocation in walker.Invocations)
                    {
                        if (SyntaxNodeExt.IsExecutedBefore(location, invocation) == Result.Yes &&
                            IsDisposing(invocation, local, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }

                using (var walker = UsingStatementWalker.Borrow(scope))
                {
                    foreach (var usingStatement in walker.UsingStatements)
                    {
                        if (SyntaxNodeExt.IsExecutedBefore(location, usingStatement) == Result.Yes &&
                            usingStatement.Expression is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == local.Name)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsReturned(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = ReturnValueWalker.Borrow(scope, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
            {
                foreach (var value in walker)
                {
                    var candidate = value;
                    switch (candidate)
                    {
                        case CastExpressionSyntax castExpression:
                            candidate = castExpression.Expression;
                            break;
                        case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AsExpression):
                            candidate = binary.Left;
                            break;
                    }

                    if (candidate is ObjectCreationExpressionSyntax objectCreation)
                    {
                        if (objectCreation.ArgumentList != null)
                        {
                            foreach (var argument in objectCreation.ArgumentList.Arguments)
                            {
                                if (semanticModel.TryGetSymbol(argument.Expression, cancellationToken, out ISymbol argumentSymbol) &&
                                    SymbolComparer.Equals(symbol, argumentSymbol))
                                {
                                    return true;
                                }
                            }
                        }

                        if (objectCreation.Initializer != null)
                        {
                            foreach (var expression in objectCreation.Initializer.Expressions)
                            {
                                if (semanticModel.TryGetSymbol(expression, cancellationToken, out ISymbol argumentSymbol) &&
                                    SymbolComparer.Equals(symbol, argumentSymbol))
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    var returnedSymbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
                    if (SymbolComparer.Equals(symbol, returnedSymbol))
                    {
                        return true;
                    }

                    if (candidate is InvocationExpressionSyntax invocation)
                    {
                        if (returnedSymbol == KnownSymbol.RxDisposable.Create &&
                            invocation.ArgumentList != null &&
                            invocation.ArgumentList.Arguments.TrySingle(out ArgumentSyntax argument) &&
                            argument.Expression is ParenthesizedLambdaExpressionSyntax lambda)
                        {
                            var body = lambda.Body;
                            using (var pooledInvocations = InvocationWalker.Borrow(body))
                            {
                                foreach (var disposeCandidate in pooledInvocations.Invocations)
                                {
                                    if (IsDisposing(disposeCandidate, symbol, semanticModel, cancellationToken))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedToFieldOrProperty(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<ISymbol> visited = null)
        {
            if (AssignmentExecutionWalker.FirstWith(symbol, scope, ReturnValueSearch.Recursive, semanticModel, cancellationToken, out var assignment))
            {
                var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken) ??
                           semanticModel.GetSymbolSafe((assignment.Left as ElementAccessExpressionSyntax)?.Expression, cancellationToken);
                if (left.IsEither<IParameterSymbol, ILocalSymbol>())
                {
                    using (visited = visited.IncrementUsage())
                    {
                        return visited.Add(left) &&
                               IsAssignedToFieldOrProperty(left, scope, semanticModel, cancellationToken, visited);
                    }
                }

                return left.IsEither<IFieldSymbol, IPropertySymbol>();
            }

            return false;
        }

        internal static bool IsAddedToFieldOrProperty(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<ISymbol> recursion = null)
        {
            using (var pooledInvocations = InvocationWalker.Borrow(scope))
            {
                foreach (var invocation in pooledInvocations.Invocations)
                {
                    if (TryGetArgument(invocation, out var argument) &&
                        semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol candidate)
                    {
                        if (IsAddMethod(candidate) &&
                            symbol.Equals(semanticModel.GetSymbolSafe(argument.Expression, cancellationToken)))
                        {
                            return true;
                        }

                        if (candidate.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax declaration) &&
                            candidate.TryFindParameter(argument, out var parameter))
                        {
                            using (var visited = recursion.IncrementUsage())
                            {
                                if (visited.Add(parameter) &&
                                    IsAddedToFieldOrProperty(parameter, declaration, semanticModel, cancellationToken, visited))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;

            bool TryGetArgument(InvocationExpressionSyntax invocation, out ArgumentSyntax argument)
            {
                argument = null;
                if (invocation.ArgumentList is ArgumentListSyntax argumentList)
                {
                    foreach (var candidate in argumentList.Arguments)
                    {
                        if (symbol.IsEither<ILocalSymbol, IParameterSymbol>())
                        {
                            if (candidate.Expression is IdentifierNameSyntax identifierName &&
                                identifierName.Identifier.ValueText == symbol.Name)
                            {
                                argument = candidate;
                                return true;
                            }

                            if (candidate.Expression is DeclarationExpressionSyntax declaration &&
                                declaration.Designation is SingleVariableDesignationSyntax singleVariable &&
                                singleVariable.Identifier.ValueText == symbol.Name)
                            {
                                argument = candidate;
                                return true;
                            }
                        }
                        else if (SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(candidate.Expression, cancellationToken)))
                        {
                            argument = candidate;
                            return true;
                        }
                    }
                }

                return false;
            }

            bool IsAddMethod(IMethodSymbol candidate)
            {
                switch (candidate.Name)
                {
                    case "Add":
                    case "Insert":
                    case "Push":
                    case "Enqueue":
                    case "TryAdd":
                    case "TryUpdate":
                        return candidate.DeclaringSyntaxReferences.Length == 0 &&
                               candidate.ContainingType.IsAssignableTo(KnownSymbol.IEnumerable, semanticModel.Compilation);
                }

                return false;
            }
        }

        internal static bool ShouldDispose(ILocalSymbol local, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == local.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (local.TrySingleDeclaration(cancellationToken, out var declaration))
            {
                if (declaration.Parent is UsingStatementSyntax ||
                    declaration.Parent is AnonymousFunctionExpressionSyntax)
                {
                    return false;
                }

                if (local.TryGetScope(cancellationToken, out var scope))
                {
                    return !IsReturned(local, scope, semanticModel, cancellationToken) &&
                           !IsAssignedToFieldOrProperty(local, scope, semanticModel, cancellationToken) &&
                           !IsAddedToFieldOrProperty(local, scope, semanticModel, cancellationToken) &&
                           !IsDisposedAfter(local, location, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        internal static bool ShouldDispose(IParameterSymbol parameter, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == parameter.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (parameter.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is ParameterListSyntax parameterList &&
                parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.Body is BlockSyntax block)
            {
                return !IsReturned(parameter, block, semanticModel, cancellationToken) &&
                       !IsAssignedToFieldOrProperty(parameter, block, semanticModel, cancellationToken) &&
                       !IsAddedToFieldOrProperty(parameter, block, semanticModel, cancellationToken) &&
                       !IsDisposedAfter(parameter, location, semanticModel, cancellationToken);
            }

            return false;
        }

        internal static bool IsIgnored(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.Parent is AnonymousFunctionExpressionSyntax ||
                node.Parent is UsingStatementSyntax ||
                node.Parent is EqualsValueClauseSyntax ||
                node.Parent is ReturnStatementSyntax ||
                node.Parent is ArrowExpressionClauseSyntax)
            {
                return false;
            }

            if (node.Parent is StatementSyntax)
            {
                return true;
            }

            if (node.Parent is ArgumentSyntax argument)
            {
                return IsArgumentDisposedByReturnValue(argument, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo) &&
                       IsArgumentAssignedToDisposable(argument, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return IsArgumentDisposedByInvocationReturnValue(memberAccess, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo);
            }

            return false;
        }
    }
}
