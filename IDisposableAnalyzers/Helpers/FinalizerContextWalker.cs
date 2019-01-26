namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
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
        public static FinalizerContextWalker Borrow(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return BorrowAndVisit(node, Scope.Member, semanticModel, cancellationToken, () => new FinalizerContextWalker());
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
            if (this.SemanticModel.TryGetType(node, this.CancellationToken, out var type) &&
                !type.IsValueType)
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
    }
}
