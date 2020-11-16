namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class UsagesWalker : PooledWalker<UsagesWalker>
    {
        private readonly List<IdentifierNameSyntax> usages = new();
        private ISymbol symbol = null!;
        private SemanticModel semanticModel = null!;
        private CancellationToken cancellationToken;

        internal IReadOnlyList<IdentifierNameSyntax> Usages => this.usages;

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (IsMatch())
            {
                this.usages.Add(node);
            }

            bool IsMatch()
            {
                return NameMatches() &&
                       this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var nodeSymbol) &&
                       nodeSymbol.IsEquivalentTo(this.symbol);

                bool NameMatches()
                {
                    if (string.IsNullOrEmpty(node.Identifier.ValueText))
                    {
                        return false;
                    }

                    if (node.Identifier.ValueText[0] == '@')
                    {
                        return node.Identifier.ValueText.IsParts("@", this.symbol.Name);
                    }

                    return node.Identifier.ValueText == this.symbol.Name;
                }
            }
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            // Ignoring catch clauses as they are not run in normal flow.
        }

        public override void VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            // Ignoring catch clauses as they are not run in normal flow.
        }

        internal static UsagesWalker Borrow(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                return Borrow(localOrParameter.Symbol, scope, semanticModel, cancellationToken);
            }

            return Borrow(() => new UsagesWalker());
        }

        internal static UsagesWalker Borrow(FieldOrPropertyAndDeclaration fieldOrProperty, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (fieldOrProperty.Declaration.Parent is TypeDeclarationSyntax containingType)
            {
                return Borrow(fieldOrProperty.FieldOrProperty.Symbol, containingType, semanticModel, cancellationToken);
            }

            return Borrow(() => new UsagesWalker());
        }

        internal static UsagesWalker Borrow(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new UsagesWalker());
            walker.symbol = symbol;
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            walker.Visit(scope);
            return walker;
        }

        protected override void Clear()
        {
            this.usages.Clear();
            this.symbol = null!;
            this.semanticModel = null!;
            this.cancellationToken = CancellationToken.None;
        }
    }
}
