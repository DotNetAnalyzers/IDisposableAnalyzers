namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class InvocationWalker : PooledWalker<InvocationWalker>
    {
        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();

        private InvocationWalker()
        {
        }

        internal IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            this.invocations.Add(node);
            base.VisitInvocationExpression(node);
        }

        internal static InvocationWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new InvocationWalker());

        internal void RemoveAll(Predicate<InvocationExpressionSyntax> match) => this.invocations.RemoveAll(match);

        protected override void Clear()
        {
            this.invocations.Clear();
        }
    }
}
