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

        [Obsolete("Use Recursion")]
        private static SymbolAndDeclaration<IParameterSymbol, BaseMethodDeclarationSyntax>? Target(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            if (visited.CanVisit(argument, out visited))
            {
                using (visited)
                {
                    switch (argument)
                    {
                        case { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }
                            when semanticModel.TryGetSymbol(invocation, cancellationToken, out var method) &&
                                 method.TryFindParameter(argument, out var parameter) &&
                                 method.TrySingleMethodDeclaration(cancellationToken, out var declaration):
                            return new SymbolAndDeclaration<IParameterSymbol, BaseMethodDeclarationSyntax>(parameter, declaration);

                        case { Parent: ArgumentListSyntax { Parent: ObjectCreationExpressionSyntax objectCreation } }
                            when semanticModel.TryGetSymbol(objectCreation, cancellationToken, out var ctor) &&
                                 ctor.TryFindParameter(argument, out var parameter) &&
                                 ctor.TrySingleMethodDeclaration(cancellationToken, out var declaration):
                            return new SymbolAndDeclaration<IParameterSymbol, BaseMethodDeclarationSyntax>(parameter, declaration);
                    }
                }
            }

            return null;
        }

        [Obsolete("Use recursion")]
        private static DisposableWalker CreateUsagesWalker(SymbolAndDeclaration<IParameterSymbol, BaseMethodDeclarationSyntax> target, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = BorrowAndVisit(target.Declaration, () => new DisposableWalker());
            walker.RemoveAll(x => !IsMatch(x));
            return walker;

            bool IsMatch(IdentifierNameSyntax identifierName)
            {
                if (identifierName.Identifier.ValueText == target.Symbol.Name &&
                    semanticModel.TryGetSymbol(identifierName, cancellationToken, out var symbol))
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
                if (identifierName.Identifier.ValueText == localOrParameter.Name &&
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

        private static DisposableWalker CreateUsagesWalker(LocalOrParameter localOrParameter, Recursion recursion)
        {
            if (localOrParameter.TryGetScope(recursion.CancellationToken, out var scope))
            {
                return CreateUsagesWalker(localOrParameter, scope, recursion);
            }

            return Borrow(() => new DisposableWalker());
        }

        private static DisposableWalker CreateUsagesWalker(SymbolAndDeclaration<IParameterSymbol, BaseMethodDeclarationSyntax> target, Recursion recursion)
        {
            return CreateUsagesWalker(new LocalOrParameter(target.Symbol), target.Declaration, recursion);
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
