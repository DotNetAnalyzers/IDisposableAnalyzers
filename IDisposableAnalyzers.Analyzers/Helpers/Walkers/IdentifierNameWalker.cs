namespace IDisposableAnalyzers
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class IdentifierNameWalker : PooledWalker<IdentifierNameWalker>
    {
        private readonly List<IdentifierNameSyntax> identifierNames = new List<IdentifierNameSyntax>();

        private IdentifierNameWalker()
        {
        }

        public IReadOnlyList<IdentifierNameSyntax> IdentifierNames => this.identifierNames;

        public static IdentifierNameWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new IdentifierNameWalker());

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.identifierNames.Add(node);
            base.VisitIdentifierName(node);
        }

        protected override void Clear()
        {
            this.identifierNames.Clear();
        }
    }
}