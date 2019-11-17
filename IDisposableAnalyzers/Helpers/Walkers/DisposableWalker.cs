namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
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

        private static DisposableWalker CreateUsagesWalker(LocalOrParameter localOrParameter, Recursion recursion)
        {
            if (localOrParameter.TryGetScope(recursion.CancellationToken, out var scope))
            {
                return CreateUsagesWalker(localOrParameter, scope, recursion);
            }

            return Borrow(() => new DisposableWalker());
        }

        private static DisposableWalker CreateUsagesWalker<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion)
              where TSource : SyntaxNode
              where TSymbol : ISymbol
              where TNode : SyntaxNode
        {
            if (target.TargetNode is { } node)
            {
                var walker = BorrowAndVisit(node, () => new DisposableWalker());
                walker.RemoveAll(x => !IsMatch(x));
                return walker;
            }

            return Borrow(() => new DisposableWalker());
            bool IsMatch(IdentifierNameSyntax identifierName)
            {
                if (identifierName.Identifier.ValueText == target.Symbol.Name &&
                    recursion.SemanticModel.TryGetSymbol(identifierName, recursion.CancellationToken, out var symbol))
                {
                    switch (symbol)
                    {
                        case ILocalSymbol local:
                            return local.Equals(target.Symbol);
                        case IParameterSymbol _:
                            return target.Symbol.Kind == SymbolKind.Parameter;
                    }
                }

                return false;
            }
        }

        private static DisposableWalker CreateUsagesWalker(LocalOrParameter localOrParameter, SyntaxNode node, Recursion recursion)
        {
            var walker = BorrowAndVisit(node, () => new DisposableWalker());
            walker.RemoveAll(x => !IsMatch(x));
            return walker;

            bool IsMatch(IdentifierNameSyntax identifierName)
            {
                if (identifierName.Identifier.ValueText == localOrParameter.Name &&
                    recursion.SemanticModel.TryGetSymbol(identifierName, recursion.CancellationToken, out var symbol))
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
