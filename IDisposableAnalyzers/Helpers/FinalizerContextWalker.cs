namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// A walker that finds all touched <see cref="IdentifierNameSyntax"/>.
    /// </summary>
    internal sealed class FinalizerContextWalker : ExecutionWalker<FinalizerContextWalker>
    {
        private readonly List<SyntaxNode> usedReferenceTypes = new List<SyntaxNode>();

        private FinalizerContextWalker()
        {
        }

        /// <summary>
        /// Gets the <see cref="IdentifierNameSyntax"/>s found in the scope.
        /// </summary>
        public IReadOnlyList<SyntaxNode> UsedReferenceTypes => this.usedReferenceTypes;

        /// <summary>
        /// Get a walker that has visited <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="semanticModel">The <see cref="SemanticModel"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A walker that has visited <paramref name="node"/>.</returns>
        public static FinalizerContextWalker Borrow(BaseMethodDeclarationSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = BorrowAndVisit(node, Scope.Member, semanticModel, cancellationToken, () => new FinalizerContextWalker());
            if (node is MethodDeclarationSyntax)
            {
                walker.usedReferenceTypes.RemoveAll(x => IsInIfDisposing(x));
            }

            return walker;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.TryGetMethodName(out var name) &&
                name != "Dispose")
            {
                base.VisitInvocationExpression(node);
            }
        }

        /// <inheritdoc />
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (!IsAssignedNull(node) &&
                this.SemanticModel.TryGetType(node, this.CancellationToken, out var type) &&
                type.IsReferenceType &&
                type.TypeKind != TypeKind.Error)
            {
                this.usedReferenceTypes.Add(node);
            }

            base.VisitIdentifierName(node);
        }

        /// <inheritdoc />
        protected override void Clear()
        {
            this.usedReferenceTypes.Clear();
        }

        private static bool IsAssignedNull(SyntaxNode node)
        {
            if (node.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression?.IsKind(SyntaxKind.ThisExpression) == true)
            {
                return IsAssignedNull(memberAccess);
            }

            if (node.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Left.Contains(node) &&
                assignment.Right?.IsKind(SyntaxKind.NullLiteralExpression) == true)
            {
                return true;
            }

            return false;
        }

        private static bool IsInIfDisposing(SyntaxNode node)
        {
            if (node.TryFirstAncestor(out IfStatementSyntax ifStatement))
            {
                if (ifStatement.Statement.Contains(node) &&
                    ifStatement.Condition is IdentifierNameSyntax identifierName &&
                    ifStatement.TryFirstAncestor(out MethodDeclarationSyntax methodDeclaration) &&
                    methodDeclaration.TryFindParameter(identifierName.Identifier.Text, out _))
                {
                    return true;
                }

                return IsInIfDisposing(ifStatement);
            }

            return false;
        }
    }
}
