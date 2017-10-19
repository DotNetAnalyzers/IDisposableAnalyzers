namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignmentWalker : ExecutionWalker<AssignmentWalker>
    {
        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();

        private AssignmentWalker()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);
            this.assignments.Add(node);
        }

        internal static AssignmentWalker Borrow(SyntaxNode node, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new AssignmentWalker());
            walker.SemanticModel = semanticModel;
            walker.CancellationToken = cancellationToken;
            walker.Search = search;
            walker.Visit(node);
            return walker;
        }

        internal static bool FirstForSymbol(ISymbol symbol, SyntaxNode scope, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return FirstForSymbol(symbol, scope, search, semanticModel, cancellationToken, out AssignmentExpressionSyntax _);
        }

        internal static bool FirstForSymbol(ISymbol symbol, SyntaxNode scope, Search search, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Borrow(scope, search, semanticModel, cancellationToken))
            {
                foreach (var candidate in pooledAssignments.Assignments)
                {
                    var assignedSymbol = semanticModel.GetSymbolSafe(candidate.Left, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        assignment = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool SingleForSymbol(ISymbol symbol, SyntaxNode scope, Search search, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Borrow(scope, search, semanticModel, cancellationToken))
            {
                foreach (var candidate in pooledAssignments.Assignments)
                {
                    var assignedSymbol = semanticModel.GetSymbolSafe(candidate.Left, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        if (assignment != null)
                        {
                            assignment = null;
                            return false;
                        }

                        assignment = candidate;
                    }
                }
            }

            return assignment != null;
        }

        internal static bool FirstWith(ISymbol symbol, SyntaxNode scope, Search search, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Borrow(scope, search, semanticModel, cancellationToken))
            {
                foreach (var candidate in pooledAssignments.Assignments)
                {
                    if (candidate.Right is ConditionalExpressionSyntax conditional)
                    {
                        if (SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(conditional.WhenTrue, cancellationToken)) ||
                            SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(conditional.WhenFalse, cancellationToken)))
                        {
                            assignment = candidate;
                            return true;
                        }
                    }

                    var assignedSymbol = semanticModel.GetSymbolSafe(candidate.Right, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        assignment = candidate;
                        return true;
                    }

                    if (candidate.Right is ObjectCreationExpressionSyntax objectCreation &&
                        objectCreation.ArgumentList != null &&
                        objectCreation.ArgumentList.Arguments.TryGetFirst(x => SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(x.Expression, cancellationToken)), out ArgumentSyntax _))
                    {
                        assignment = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void Clear()
        {
            this.assignments.Clear();
            this.SemanticModel = null;
            this.CancellationToken = CancellationToken.None;
            base.Clear();
        }
    }
}