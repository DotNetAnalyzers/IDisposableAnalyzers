namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class DisposeWalker : ExecutionWalker<DisposeWalker>, IReadOnlyList<InvocationExpressionSyntax>
    {
        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
        private readonly List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();

        private DisposeWalker()
        {
            this.Scope = Scope.Recursive;
        }

        public int Count => this.invocations.Count;

        public InvocationExpressionSyntax this[int index] => this.invocations[index];

        public IEnumerator<InvocationExpressionSyntax> GetEnumerator() => this.invocations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.invocations).GetEnumerator();

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            if (this.SemanticModel.TryGetSymbol(node, KnownSymbol.IDisposable.Dispose, this.CancellationToken, out var dispose) &&
                dispose.Parameters.Length == 0)
            {
                this.invocations.Add(node);
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.identifiers.Add(node);
            base.VisitIdentifierName(node);
        }

        internal static DisposeWalker Borrow(ITypeSymbol type, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, semanticModel.Compilation))
            {
                return Borrow(semanticModel, cancellationToken);
            }

            if (DisposeMethod.TryFindFirst(type, semanticModel.Compilation, Search.Recursive, out var disposeMethod))
            {
                return Borrow(disposeMethod, semanticModel, cancellationToken);
            }

            return Borrow(semanticModel, cancellationToken);
        }

        internal static DisposeWalker Borrow(IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Borrow(semanticModel, cancellationToken);
            foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
            {
                pooled.Visit(reference.GetSyntax(cancellationToken));
            }

            return pooled;
        }

        internal Result IsMemberDisposed(ISymbol member)
        {
            foreach (var invocation in this.invocations)
            {
                if (DisposeCall.IsDisposing(invocation, member, this.SemanticModel, this.CancellationToken))
                {
                    return Result.Yes;
                }
            }

            foreach (var name in this.identifiers)
            {
                if (SymbolComparer.Equals(member, this.SemanticModel.GetSymbolSafe(name, this.CancellationToken)))
                {
                    return Result.AssumeYes;
                }
            }

            return Result.No;
        }

        protected override void Clear()
        {
            this.invocations.Clear();
            this.identifiers.Clear();
            base.Clear();
        }

        private static DisposeWalker Borrow(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Borrow(() => new DisposeWalker());
            pooled.SemanticModel = semanticModel;
            pooled.CancellationToken = cancellationToken;
            return pooled;
        }
    }
}
