namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class DisposableWalker : PooledWalker<DisposableWalker>
    {
        private readonly List<IdentifierNameSyntax> usages = new List<IdentifierNameSyntax>();

        public void RemoveAll(Predicate<IdentifierNameSyntax> match) => this.usages.RemoveAll(match);

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.usages.Add(node);
        }

        public static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (IsReturned(usage))
                    {
                        return false;
                    }

                    if (IsAssignedToFieldOrProperty(usage, semanticModel, cancellationToken, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsAssigned(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty first)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (IsAssignedToFieldOrProperty(usage, semanticModel, cancellationToken, out first))
                    {
                        return true;
                    }
                }
            }

            return false;
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
                       symbol.Equals(localOrParameter.Symbol);
            }
        }

        private static bool IsReturned(ExpressionSyntax candidate)
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
                    return IsReturned((ExpressionSyntax)candidate.Parent);
            }

            if (candidate.Parent is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList)
            {
                if (argumentList.Parent is ObjectCreationExpressionSyntax objectCreation)
                {
                    return IsReturned(objectCreation);
                }
            }

            return false;
        }

        private static bool IsAssignedToFieldOrProperty(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty fieldOrProperty)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return IsAssignedToFieldOrProperty((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, out fieldOrProperty);
            }

            if (candidate.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Right.Contains(candidate) &&
                semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol symbol))
            {
                return FieldOrProperty.TryCreate(symbol, out fieldOrProperty);
            }

            return false;
        }
    }
}
