namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class UsingStatementWalker : PooledWalker<UsingStatementWalker>
    {
        private readonly List<UsingStatementSyntax> usingStatements = new List<UsingStatementSyntax>();

        private UsingStatementWalker()
        {
        }

        internal IReadOnlyList<UsingStatementSyntax> UsingStatements => this.usingStatements;

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            this.usingStatements.Add(node);
            base.VisitUsingStatement(node);
        }

        internal static UsingStatementWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new UsingStatementWalker());

        protected override void Clear()
        {
            this.usingStatements.Clear();
        }
    }
}
