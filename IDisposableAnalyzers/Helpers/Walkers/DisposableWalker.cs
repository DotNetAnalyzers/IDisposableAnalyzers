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

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.usages.Add(node);
        }

        internal void RemoveAll(Predicate<IdentifierNameSyntax> match) => this.usages.RemoveAll(match);

        protected override void Clear()
        {
            this.usages.Clear();
        }

        [Obsolete("Use recursion")]
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
                if (identifierName.Identifier.Text == localOrParameter.Name &&
                    semanticModel.TryGetSymbol(identifierName, cancellationToken, out var symbol))
                {
                    switch (symbol)
                    {
                        case ILocalSymbol local:
                            return local.Equals(localOrParameter.Symbol);
                        case IParameterSymbol _:
                            return localOrParameter.Symbol.Kind == SymbolKind.Parameter;
                    }
                }

                return false;
            }
        }
    }
}
