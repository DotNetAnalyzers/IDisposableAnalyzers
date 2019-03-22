namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class DisposableWalker : PooledWalker<DisposableWalker>
    {
        private readonly List<IdentifierNameSyntax> usages = new List<IdentifierNameSyntax>();

        public static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Returns(usage, semanticModel, cancellationToken, null))
                    {
                        return false;
                    }

                    if (Assigns(usage, semanticModel, cancellationToken, null, out _))
                    {
                        return false;
                    }

                    if (Stores(usage, semanticModel, cancellationToken, null, out _))
                    {
                        return false;
                    }

                    if (Disposes(usage, semanticModel, cancellationToken, null))
                    {
                        return false;
                    }
                }
            }

            if (localOrParameter.Symbol is ILocalSymbol local &&
                local.TrySingleDeclaration(cancellationToken, out SingleVariableDesignationSyntax designation) &&
                designation.Parent is DeclarationExpressionSyntax declaration &&
                declaration.Parent is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method) &&
                method.TryFindParameter(argument, out var parameter) &&
                LocalOrParameter.TryCreate(parameter, out localOrParameter))
            {
                return ShouldDispose(localOrParameter, semanticModel, cancellationToken);
            }

            return true;
        }

        public static bool Returns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Returns(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool Assigns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out FieldOrProperty first)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Assigns(usage, semanticModel, cancellationToken, visited, out first))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out ISymbol container)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
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

        public static bool DisposesAfter(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is VariableDeclarationSyntax variableDeclaration &&
                variableDeclaration.Parent is UsingStatementSyntax)
            {
                return true;
            }

            using (var walker = CreateWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (location.IsExecutedBefore(usage).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                        Disposes(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool DisposesBefore(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            using (var walker = CreateWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (usage.IsExecutedBefore(location).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                        Disposes(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool Disposes(ILocalSymbol local, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
               declaration.Parent is VariableDeclarationSyntax variableDeclaration &&
               variableDeclaration.Parent is UsingStatementSyntax)
            {
                return true;
            }

            using (var walker = CreateWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Disposes(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool DisposedByReturnValue(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (candidate.Parent is ArgumentListSyntax argumentList &&
                semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method))
            {
                if (method.ContainingType == KnownSymbol.SingleAssignmentDisposable ||
                    method.ContainingType == KnownSymbol.RxDisposable ||
                    method.ContainingType == KnownSymbol.CompositeDisposable)
                {
                    return true;
                }

                if (method.MethodKind == MethodKind.Constructor &&
                    Disposable.IsAssignableFrom(method.ContainingType, semanticModel.Compilation))
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
                if (CanVisit(parameterSyntax, visited, out visited))
                {
                    using (visited)
                    {
                        using (var walker = CreateWalker(new LocalOrParameter(candidate), semanticModel, cancellationToken))
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

        public void RemoveAll(Predicate<IdentifierNameSyntax> match) => this.usages.RemoveAll(match);

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.usages.Add(node);
        }

        protected override void Clear()
        {
            this.usages.Clear();
        }

        private static DisposableWalker CreateWalker(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                var walker = BorrowAndVisit(scope, () => new DisposableWalker());
                walker.RemoveAll(x => !IsMatch(x));
                return walker;
            }

            return Borrow(() => new DisposableWalker());

            bool IsMatch(IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.Text == localOrParameter.Name &&
                       semanticModel.TryGetSymbol(identifierName, cancellationToken, out ISymbol symbol) &&
                       symbol.Equals(localOrParameter.Symbol.OriginalDefinition);
            }
        }

        private static bool Returns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ArrowExpressionClause:
                    return true;
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                case SyntaxKind.CollectionInitializerExpression:
                case SyntaxKind.ObjectCreationExpression:
                    return Returns((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is ObjectCreationExpressionSyntax objectCreation:
                    return Returns(objectCreation, semanticModel, cancellationToken, visited);
                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var localOrParameter):
                    if (CanVisit(candidate, visited, out visited))
                    {
                        using (visited)
                        {
                            return Returns(localOrParameter, semanticModel, cancellationToken, visited);
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool Assigns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out FieldOrProperty fieldOrProperty)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return Assigns((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited, out fieldOrProperty);
            }

            switch (candidate.Parent)
            {
                case AssignmentExpressionSyntax assignment:
                    return assignment.Right.Contains(candidate) &&
                           semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol assignedSymbol) &&
                           FieldOrProperty.TryCreate(assignedSymbol, out fieldOrProperty);
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                  invocation.IsPotentialThisOrBase() &&
                                                  semanticModel.TryGetSymbol(invocation, cancellationToken, out IMethodSymbol method) &&
                                                  method.TryFindParameter(argument, out var parameter) &&
                                                  LocalOrParameter.TryCreate(parameter, out var localOrParameter):
                    if (CanVisit(candidate, visited, out visited))
                    {
                        using (visited)
                        {
                            return Assigns(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                        }
                    }

                    return false;

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var localOrParameter):
                    if (CanVisit(candidate, visited, out visited))
                    {
                        using (visited)
                        {
                            return Assigns(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                        }
                    }

                    return false;

                default:
                    return false;
            }
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
                                                  DisposedByReturnValue(argument, semanticModel, cancellationToken, visited):
                    return StoresOrAssigns(invocationOrObjectCreation, out container);
                case ArgumentSyntax argument when argument.Parent is TupleExpressionSyntax tupleExpression:
                    return Stores(tupleExpression, semanticModel, cancellationToken, visited, out container) ||
                           Assigns(tupleExpression, semanticModel, cancellationToken, visited, out _);
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                  semanticModel.TryGetSymbol(invocation, cancellationToken, out IMethodSymbol method):
                    {
                        if (method.DeclaringSyntaxReferences.IsEmpty)
                        {
                            if (method.ContainingType.IsAssignableTo(KnownSymbol.IEnumerable, semanticModel.Compilation) &&
                               invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                            {
                                return semanticModel.TryGetSymbol(memberAccess.Expression, cancellationToken, out container);
                            }

                            if (method == KnownSymbol.Tuple.Create)
                            {
                                return StoresOrAssigns(invocation, out container);
                            }

                            container = null;
                            return false;
                        }
                        else if (method.TryFindParameter(argument, out var parameter) &&
                                 LocalOrParameter.TryCreate(parameter, out var localOrParameter))
                        {
                            if (CanVisit(candidate, visited, out visited))
                            {
                                using (visited)
                                {
                                    if (invocation.IsPotentialThisOrBase() &&
                                        Stores(localOrParameter, semanticModel, cancellationToken, visited, out container))
                                    {
                                        return true;
                                    }

                                    if (Stores(localOrParameter, semanticModel, cancellationToken, visited, out _))
                                    {
                                        _ = StoresOrAssigns(invocation, out container);
                                        return true;
                                    }

                                    if (Assigns(localOrParameter, semanticModel, cancellationToken, visited, out var fieldOrProperty) &&
                                        semanticModel.IsAccessible(candidate.SpanStart, fieldOrProperty.Symbol))
                                    {
                                        return StoresOrAssigns(invocation, out container);
                                    }
                                }
                            }

                            container = null;
                            return false;
                        }

                        container = null;
                        return false;
                    }

                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is ObjectCreationExpressionSyntax objectCreation &&
                                                  semanticModel.TryGetSymbol(objectCreation, cancellationToken, out IMethodSymbol method):
                    {
                        if (method.DeclaringSyntaxReferences.IsEmpty)
                        {
                            if (method.ContainingType.FullName().StartsWith("System.Tuple`") ||
                                method.ContainingType == KnownSymbol.CompositeDisposable ||
                                method.ContainingType == KnownSymbol.SerialDisposable)
                            {
                                return StoresOrAssigns(objectCreation, out container);
                            }

                            container = null;
                            return false;
                        }
                        else if (method.TryFindParameter(argument, out var parameter) &&
                                 LocalOrParameter.TryCreate(parameter, out var localOrParameter))
                        {
                            if (CanVisit(candidate, visited, out visited))
                            {
                                using (visited)
                                {
                                    if (Stores(localOrParameter, semanticModel, cancellationToken, visited, out container))
                                    {
                                        _ = StoresOrAssigns(objectCreation, out container);
                                        return true;
                                    }

                                    if (Assigns(localOrParameter, semanticModel, cancellationToken, visited, out var fieldOrProperty) &&
                                        semanticModel.IsAccessible(candidate.SpanStart, fieldOrProperty.Symbol))
                                    {
                                        return StoresOrAssigns(objectCreation, out container);
                                    }
                                }
                            }
                        }

                        container = null;
                        return false;
                    }

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out container) &&
                                                                    LocalOrParameter.TryCreate(container, out var local):
                    if (CanVisit(candidate, visited, out visited))
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

        private static bool Disposes(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.UsingStatement:
                    return true;
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ParenthesizedExpression:
                    return Disposes((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case ConditionalAccessExpressionSyntax conditionalAccess when conditionalAccess.WhenNotNull is InvocationExpressionSyntax invocation:
                    return IsDispose(invocation);
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Parent is InvocationExpressionSyntax invocation:
                    return IsDispose(invocation);
                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ILocalSymbol assignedSymbol):
                    if (CanVisit(candidate, visited, out visited))
                    {
                        using (visited)
                        {
                            return Disposes(assignedSymbol, semanticModel, cancellationToken, visited);
                        }
                    }

                    return false;
            }

            return false;

            bool IsDispose(InvocationExpressionSyntax invocation)
            {
                return invocation.ArgumentList is ArgumentListSyntax argumentList &&
                        argumentList.Arguments.Count == 0 &&
                        invocation.TryGetMethodName(out var name) &&
                        name == "Dispose";
            }
        }

        private static bool CanVisit(SyntaxNode node, PooledSet<(string, SyntaxNode)> visited, out PooledSet<(string, SyntaxNode)> incremented, [CallerMemberName] string caller = null)
        {
            incremented = visited.IncrementUsage();
            return incremented.Add((caller ?? string.Empty, node));
        }
    }
}
