namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class InvocationWalker : ExecutionWalker<InvocationWalker>, IReadOnlyList<InvocationExpressionSyntax>
    {
        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();

        private InvocationWalker()
        {
            this.Search = Search.TopLevel;
        }

        public IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

        public int Count => this.invocations.Count;

        public InvocationExpressionSyntax this[int index] => this.invocations[index];

        public IEnumerator<InvocationExpressionSyntax> GetEnumerator() => this.invocations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.invocations).GetEnumerator();

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            this.invocations.Add(node);
            base.VisitInvocationExpression(node);
        }

        internal static InvocationWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new InvocationWalker());

        protected override void Clear()
        {
            this.invocations.Clear();
            base.Clear();
        }
    }
}