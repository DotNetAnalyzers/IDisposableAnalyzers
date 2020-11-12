namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class DisposeWalker : ExecutionWalker<DisposeWalker>
    {
        private readonly List<DisposeCall> invocations = new List<DisposeCall>();
        private readonly List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();

        private DisposeWalker()
        {
            this.SearchScope = SearchScope.Instance;
        }

        internal IReadOnlyList<DisposeCall> Invocations => this.invocations;

        internal IReadOnlyList<IdentifierNameSyntax> Identifiers => this.identifiers;

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (DisposeCall.MatchAny(node, this.SemanticModel, this.CancellationToken) is { } dispose)
            {
                this.invocations.Add(dispose);
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.identifiers.Add(node);
            base.VisitIdentifierName(node);
        }

        internal static DisposeWalker Borrow(INamedTypeSymbol type, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (type.IsAssignableTo(KnownSymbol.IDisposable, semanticModel.Compilation) &&
                DisposeMethod.FindFirst(type, semanticModel.Compilation, Search.Recursive) is { } disposeMethod &&
                disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax? declaration))
            {
                return BorrowAndVisit(declaration, SearchScope.Instance, type, semanticModel, () => new DisposeWalker(), cancellationToken);
            }

            if (type.IsAssignableTo(KnownSymbol.IAsyncDisposable, semanticModel.Compilation) &&
                type.TryFindFirstMethod(x => x is { Parameters: { Length: 0 } } && x == KnownSymbol.IAsyncDisposable.DisposeAsync, out var disposeAsync) &&
                disposeAsync.TrySingleDeclaration(cancellationToken, out declaration))
            {
                return BorrowAndVisit(declaration, SearchScope.Instance, type, semanticModel, () => new DisposeWalker(), cancellationToken);
            }

            return Borrow(() => new DisposeWalker());
        }

        internal static DisposeWalker Borrow(IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax? declaration))
            {
                return BorrowAndVisit(declaration, SearchScope.Instance, semanticModel, cancellationToken, () => new DisposeWalker());
            }

            return Borrow(() => new DisposeWalker());
        }

        internal static DisposeWalker Borrow(MethodDeclarationSyntax disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return BorrowAndVisit(disposeMethod, SearchScope.Instance, semanticModel, cancellationToken, () => new DisposeWalker());
        }

        internal bool IsMemberDisposed(ISymbol member)
        {
            foreach (var invocation in this.invocations)
            {
                if (invocation.IsDisposing(member, this.SemanticModel, this.CancellationToken))
                {
                    return true;
                }
            }

            if (member is IPropertySymbol { OverriddenProperty: { } overridden })
            {
                return this.IsMemberDisposed(overridden);
            }

            foreach (var identifier in this.identifiers)
            {
                if (member.Name == identifier.Identifier.ValueText &&
                    this.SemanticModel.TryGetSymbol(identifier, this.CancellationToken, out var candidate) &&
                    SymbolComparer.Equal(member, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void Clear()
        {
            this.invocations.Clear();
            this.identifiers.Clear();
            base.Clear();
        }
    }
}
