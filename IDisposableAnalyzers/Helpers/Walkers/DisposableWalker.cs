namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker : PooledWalker<DisposableWalker>
    {
        private readonly List<IdentifierNameSyntax> usages = new List<IdentifierNameSyntax>();

        public void RemoveAll(Predicate<IdentifierNameSyntax> match) => this.usages.RemoveAll(match);

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.usages.Add(node);
        }

        protected override void Clear()
        {
            this.usages.Clear();
        }

        private static DisposableWalker CreateUsagesWalker(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                var walker = BorrowAndVisit(scope, () => new DisposableWalker());
                walker.RemoveAll(x => !IsMatch(x));
                return walker;
            }

            return Borrow(() => new DisposableWalker());

            bool IsMatch(IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.Text == localOrParameter.Name &&
                       semanticModel.TryGetSymbol(identifierName, cancellationToken, out ISymbol symbol) &&
                       symbol.Equals(localOrParameter.Symbol.OriginalDefinition);
            }
        }
    }
}
