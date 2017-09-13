namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class InvocationWalker : ExecutionWalker, IReadOnlyList<InvocationExpressionSyntax>
    {
        private static readonly Pool<InvocationWalker> Pool = new Pool<InvocationWalker>(
            () => new InvocationWalker(),
            x =>
            {
                x.invocations.Clear();
                x.Clear();
            });

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

        internal static Pool<InvocationWalker>.Pooled Create(SyntaxNode node)
        {
            var pooled = Pool.GetOrCreate();
            if (node != null)
            {
                pooled.Item.Visit(node);
            }

            return pooled;
        }
    }
}