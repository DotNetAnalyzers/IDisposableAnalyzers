namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class YieldStatementWalker : PooledWalker<YieldStatementWalker>
    {
        private readonly List<YieldStatementSyntax> yieldStatements = new List<YieldStatementSyntax>();

        private YieldStatementWalker()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<YieldStatementSyntax> YieldStatements => this.yieldStatements;

        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            this.yieldStatements.Add(node);
            base.VisitYieldStatement(node);
        }

        internal static YieldStatementWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new YieldStatementWalker());

        internal static bool Any(MethodDeclarationSyntax methodDeclaration)
        {
            using (var walker = Borrow(methodDeclaration))
            {
                return walker.yieldStatements.Count > 0;
            }
        }

        protected override void Clear()
        {
            this.yieldStatements.Clear();
        }
    }
}
