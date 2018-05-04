namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignmentExecutionWalker : ExecutionWalker<AssignmentExecutionWalker>
    {
        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();
        private readonly List<ArgumentSyntax> arguments = new List<ArgumentSyntax>();
        private readonly List<LocalDeclarationStatementSyntax> localDeclarations = new List<LocalDeclarationStatementSyntax>();

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

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            this.localDeclarations.Add(node);
            base.VisitLocalDeclarationStatement(node);
        }

        internal static AssignmentExecutionWalker Borrow(SyntaxNode node, Scope scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new AssignmentExecutionWalker());
            walker.SemanticModel = semanticModel;
            walker.CancellationToken = cancellationToken;
            walker.Scope = scope;
            walker.Visit(node);
            return walker;
        }

        internal static bool FirstForSymbol(ISymbol symbol, SyntaxNode node, Scope scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return FirstForSymbol(symbol, node, scope, semanticModel, cancellationToken, out _);
        }

        internal static bool FirstForSymbol(ISymbol symbol, SyntaxNode node, Scope scope, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                node == null)
            {
                return false;
            }

            using (var walker = Borrow(node, scope, semanticModel, cancellationToken))
            {
                foreach (var candidate in walker.Assignments)
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

        internal static bool SingleForSymbol(ISymbol symbol, SyntaxNode node, Scope scope, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                node == null)
            {
                return false;
            }

            using (var walker = Borrow(node, scope, semanticModel, cancellationToken))
            {
                foreach (var candidate in walker.Assignments)
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

        internal static bool FirstWith(ISymbol symbol, SyntaxNode scope, Search search, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment, PooledSet<ISymbol> recursion = null)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var walker = Borrow(scope, Scope.Member, semanticModel, cancellationToken))
            {
                foreach (var candidate in walker.assignments)
                {
                    if (IsMatch(symbol, candidate.Right, semanticModel, cancellationToken))
                    {
                        assignment = candidate;
                        return true;
                    }
                }

                foreach (var declaration in walker.localDeclarations)
                {
                    if (declaration.Declaration is VariableDeclarationSyntax variableDeclaration &&
                        variableDeclaration.Variables.TryFirst(x => x.Initializer != null, out var variable) &&
                        IsMatch(symbol, variable.Initializer.Value, semanticModel, cancellationToken) &&
                        semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken) is ILocalSymbol local)
                    {
                        using (var visited = recursion.IncrementUsage())
                        {
                            if (visited.Add(local) &&
                                FirstWith(local, scope, search, semanticModel, cancellationToken, out assignment, visited))
                            {
                                return true;
                            }
                        }
                    }
                }

                if (search != Search.TopLevel)
                {
                    foreach (var argument in walker.arguments)
                    {
                        if (argument.Expression is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == symbol.Name &&
                            semanticModel.GetSymbolSafe(argument.Parent?.Parent, cancellationToken) is IMethodSymbol method &&
                            method.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax methodDeclaration) &&
                            method.TryFindParameter(argument, out var parameter))
                        {
                            using (var visited = recursion.IncrementUsage())
                            {
                                if (visited.Add(parameter) &&
                                    FirstWith(parameter, methodDeclaration, search, semanticModel, cancellationToken, out assignment, visited))
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

        protected override void Clear()
        {
            this.assignments.Clear();
            this.arguments.Clear();
            this.SemanticModel = null;
            this.CancellationToken = CancellationToken.None;
            base.Clear();
        }

        private static bool IsMatch(ISymbol symbol, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (expression)
            {
                case ConditionalExpressionSyntax conditional:
                    return IsMatch(symbol, conditional.WhenTrue, semanticModel, cancellationToken) ||
                           IsMatch(symbol, conditional.WhenFalse, semanticModel, cancellationToken);
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression):
                    return IsMatch(symbol, binary.Left, semanticModel, cancellationToken) ||
                           IsMatch(symbol, binary.Right, semanticModel, cancellationToken);
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AsExpression):
                    return IsMatch(symbol, binary.Left, semanticModel, cancellationToken);
                case CastExpressionSyntax cast:
                    return IsMatch(symbol, cast.Expression, semanticModel, cancellationToken);
                case ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList != null && objectCreation.ArgumentList.Arguments.TryFirst(x => SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(x.Expression, cancellationToken)), out ArgumentSyntax _):
                    return true;
                default:
                    if (symbol.IsEither<ILocalSymbol, IParameterSymbol>())
                    {
                        return expression is IdentifierNameSyntax identifierName &&
                               identifierName.Identifier.ValueText == symbol.Name &&
                               SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(expression, cancellationToken));
                    }

                    return SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(expression, cancellationToken));
            }
        }
    }
}
