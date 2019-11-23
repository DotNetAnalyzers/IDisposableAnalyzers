namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class DisposeWalker : ExecutionWalker<DisposeWalker>
    {
        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
        private readonly List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();

        private DisposeWalker()
        {
            this.SearchScope = SearchScope.Instance;
        }

        internal IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

        internal IReadOnlyList<IdentifierNameSyntax> Identifiers => this.identifiers;

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

        internal static DisposeWalker Borrow(INamedTypeSymbol type, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (type.IsAssignableTo(KnownSymbol.IDisposable, semanticModel.Compilation) &&
                DisposeMethod.TryFindFirst(type, semanticModel.Compilation, Search.Recursive, out var disposeMethod) &&
                disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax? declaration))
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
            if (disposeMethod != null)
            {
                return BorrowAndVisit(disposeMethod, SearchScope.Instance, semanticModel, cancellationToken, () => new DisposeWalker());
            }

            return Borrow(() => new DisposeWalker());
        }

        internal static DisposeWalker Borrow(SyntaxNode scope, SearchScope search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (scope != null)
            {
                return BorrowAndVisit(scope, search, semanticModel, cancellationToken, () => new DisposeWalker());
            }

            return Borrow(() => new DisposeWalker());
        }

        internal void RemoveAll(Predicate<InvocationExpressionSyntax> match) => this.invocations.RemoveAll(match);

        internal void RemoveAll(Predicate<IdentifierNameSyntax> match) => this.identifiers.RemoveAll(match);

        internal Result IsMemberDisposed(ISymbol member)
        {
            foreach (var invocation in this.invocations)
            {
                if (DisposeCall.IsDisposing(invocation, member, this.SemanticModel, this.CancellationToken))
                {
                    return Result.Yes;
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
                     member.Equals(candidate))
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
    }
}
