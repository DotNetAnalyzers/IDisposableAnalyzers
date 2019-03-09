namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
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

        internal static bool IsNop(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.TryGetSymbol(candidate, cancellationToken, out ISymbol symbol) &&
                FieldOrProperty.TryCreate(symbol, out var fieldOrProperty) &&
                fieldOrProperty.IsStatic &&
                IsAssignableFrom(fieldOrProperty.Type, semanticModel.Compilation))
            {
                if (fieldOrProperty.Type == KnownSymbol.Task ||
                    symbol == KnownSymbol.RxDisposable.Empty)
                {
                    return true;
                }

                using (var walker = ReturnValueWalker.Borrow(candidate, ReturnValueSearch.Recursive, semanticModel, cancellationToken))
                {
                    if (walker.Count > 0)
                    {
                        return walker.TrySingle(out var value) &&
                               semanticModel.TryGetType(value, cancellationToken, out var type) &&
                               IsNop(type);
                    }
                }

                using (var walker = AssignedValueWalker.Borrow(symbol, semanticModel, cancellationToken))
                {
                    return walker.TrySingle(out var value) &&
                           semanticModel.TryGetType(value, cancellationToken, out var type) &&
                           IsNop(type);
                }
            }

            return false;

            bool IsNop(ITypeSymbol type)
            {
                return type.IsSealed &&
                       type.BaseType == KnownSymbol.Object &&
                       type.TryFindSingleMethod("Dispose", out var disposeMethod) &&
                       disposeMethod.Parameters.Length == 0 &&
                       disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax declaration) &&
                       declaration.Body is BlockSyntax body &&
                       body.Statements.Count == 0;
            }
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
                        else if (semanticModel.TryGetSymbol(candidate.Expression, cancellationToken, out ISymbol candidateSymbol) &&
                                 symbol.Equals(candidateSymbol))
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

        internal static bool ShouldDispose(LocalOrParameter localOrParameter, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (localOrParameter.Symbol)
            {
                case ILocalSymbol local:
                    return ShouldDispose(local, location, semanticModel, cancellationToken);
                case IParameterSymbol parameter:
                    return ShouldDispose(parameter, location, semanticModel, cancellationToken);
            }

            throw new InvalidOperationException("Should never get here.");
        }

        internal static bool ShouldDispose(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
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

                if (LocalOrParameter.TryCreate(local, out var localOrParameter) &&
                    local.TryGetScope(cancellationToken, out var scope))
                {
                    return DisposableWalker.ShouldDispose(localOrParameter, semanticModel, cancellationToken) &&
                           !IsAddedToFieldOrProperty(local, scope, semanticModel, cancellationToken) &&
                           !IsDisposedAfter(local, location, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        internal static bool ShouldDispose(IParameterSymbol parameter, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == parameter.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (parameter.RefKind == RefKind.None &&
                parameter.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is ParameterListSyntax parameterList &&
                parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.Body is BlockSyntax block &&
                LocalOrParameter.TryCreate(parameter, out var localOrParameter))
            {
                return DisposableWalker.ShouldDispose(localOrParameter, semanticModel, cancellationToken) &&
                       !IsAddedToFieldOrProperty(parameter, block, semanticModel, cancellationToken) &&
                       !IsDisposedAfter(parameter, location, semanticModel, cancellationToken);
            }

            return false;
        }
    }
}
