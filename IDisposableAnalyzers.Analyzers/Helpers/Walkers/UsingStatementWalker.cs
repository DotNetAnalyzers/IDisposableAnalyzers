namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class UsingStatementWalker : PooledWalker<UsingStatementWalker>
    {
        private readonly List<UsingStatementSyntax> usingStatements = new List<UsingStatementSyntax>();

        private UsingStatementWalker()
        {
        }

        public IReadOnlyList<UsingStatementSyntax> UsingStatements => this.usingStatements;

        public static UsingStatementWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new UsingStatementWalker());

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            this.usingStatements.Add(node);
            base.VisitUsingStatement(node);
        }

        protected override void Clear()
        {
            this.usingStatements.Clear();
        }
    }
}