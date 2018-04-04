namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignmentExecutionWalker : ExecutionWalker<AssignmentExecutionWalker>
    {
        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();
        private readonly List<ArgumentSyntax> arguments = new List<ArgumentSyntax>();

        private AssignmentExecutionWalker()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            this.assignments.Add(node);
            base.VisitAssignmentExpression(node);
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            this.arguments.Add(node);
            base.VisitArgument(node);
        }

        internal static AssignmentExecutionWalker Borrow(SyntaxNode node, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new AssignmentExecutionWalker());
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

            using (var walker = Borrow(scope, Search.TopLevel, semanticModel, cancellationToken))
            {
                foreach (var candidate in walker.Assignments)
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

                    if (candidate.Right is BinaryExpressionSyntax binary &&
                        binary.IsKind(SyntaxKind.CoalesceExpression))
                    {
                        if (SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(binary.Left, cancellationToken)) ||
                            SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(binary.Right, cancellationToken)))
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
                        objectCreation.ArgumentList.Arguments.TryFirst(x => SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(x.Expression, cancellationToken)), out ArgumentSyntax _))
                    {
                        assignment = candidate;
                        return true;
                    }
                }

                if (search == Search.Recursive)
                {
                    foreach (var argument in walker.arguments)
                    {
                        if (argument.Expression is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == symbol.Name &&
                            semanticModel.GetSymbolSafe(argument.Parent?.Parent, cancellationToken) is IMethodSymbol method &&
                            method.TrySingleDeclaration(cancellationToken, out var methodDeclaration) &&
                            methodDeclaration.TryGetMatchingParameter(argument, out var parameter) &&
                            FirstWith(semanticModel.GetDeclaredSymbolSafe(parameter, cancellationToken), methodDeclaration, search, semanticModel, cancellationToken, out assignment))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected override void Clear()
        {
            this.assignments.Clear();
            this.arguments.Clear();
            this.SemanticModel = null;
            this.CancellationToken = CancellationToken.None;
            base.Clear();
        }
    }
}
