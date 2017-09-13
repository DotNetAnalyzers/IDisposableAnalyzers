namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static Result IsMemberDisposed(ISymbol member, TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsMemberDisposed(member, semanticModel.GetDeclaredSymbolSafe(context, cancellationToken), semanticModel, cancellationToken);
        }

        internal static Result IsMemberDisposed(ISymbol member, ITypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(member is IFieldSymbol ||
                  member is IPropertySymbol) ||
                  context == null)
            {
                return Result.Unknown;
            }

            using (var pooled = DisposeWalker.Create(context, semanticModel, cancellationToken))
            {
                return pooled.Item.IsMemberDisposed(member);
            }
        }

        internal static bool IsMemberDisposed(ISymbol member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member == null ||
                disposeMethod == null)
            {
                return false;
            }

            foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
            {
                var node = reference.GetSyntax(cancellationToken) as MethodDeclarationSyntax;
                using (var pooled = DisposeWalker.Create(disposeMethod, semanticModel, cancellationToken))
                {
                    foreach (var invocation in pooled.Item)
                    {
                        if (IsDisposing(invocation, member, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }

                using (var pooled = IdentifierNameWalker.Create(node))
                {
                    foreach (var identifier in pooled.Item.IdentifierNames)
                    {
                        var memberAccess = identifier.Parent as MemberAccessExpressionSyntax;
                        if (memberAccess?.Expression is BaseExpressionSyntax)
                        {
                            var baseMethod = semanticModel.GetSymbolSafe(identifier, cancellationToken) as IMethodSymbol;
                            if (baseMethod?.Name == "Dispose")
                            {
                                if (IsMemberDisposed(member, baseMethod, semanticModel, cancellationToken))
                                {
                                    return true;
                                }
                            }
                        }

                        if (identifier.Identifier.ValueText != member.Name)
                        {
                            continue;
                        }

                        var symbol = semanticModel.GetSymbolSafe(identifier, cancellationToken);
                        if (member.Equals(symbol) || (member as IPropertySymbol)?.OverriddenProperty?.Equals(symbol) == true)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsDisposing(InvocationExpressionSyntax invocation, ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
            if (method == null ||
                method.Parameters.Length != 0 ||
                method != KnownSymbol.IDisposable.Dispose)
            {
                return false;
            }

            if (TryGetDisposedRootMember(invocation, semanticModel, cancellationToken, out ExpressionSyntax disposed))
            {
                if (SymbolComparer.Equals(member, semanticModel.GetSymbolSafe(disposed, cancellationToken)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetDisposedRootMember(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax disposedMember)
        {
            if (MemberPath.TryFindRootMember(disposeCall, out disposedMember))
            {
                var property = semanticModel.GetSymbolSafe(disposedMember, cancellationToken) as IPropertySymbol;
                if (property == null ||
                    property.IsAutoProperty(cancellationToken))
                {
                    return true;
                }

                if (property.GetMethod == null)
                {
                    return false;
                }

                foreach (var reference in property.GetMethod.DeclaringSyntaxReferences)
                {
                    var node = reference.GetSyntax(cancellationToken);
                    using (var pooled = ReturnValueWalker.Create(node, Search.TopLevel, semanticModel, cancellationToken))
                    {
                        if (pooled.Item.Count == 0)
                        {
                            return false;
                        }

                        return MemberPath.TryFindRootMember(pooled.Item[0], out disposedMember);
                    }
                }
            }

            return false;
        }

        internal sealed class DisposeWalker : ExecutionWalker, IReadOnlyList<InvocationExpressionSyntax>
        {
            private static readonly Pool<DisposeWalker> Pool = new Pool<DisposeWalker>(
                () => new DisposeWalker(),
                x =>
                    {
                        x.invocations.Clear();
                        x.identifiers.Clear();
                        x.Clear();
                    });

            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
            private readonly List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();

            private DisposeWalker()
            {
                this.Search = Search.Recursive;
            }

            public int Count => this.invocations.Count;

            public InvocationExpressionSyntax this[int index] => this.invocations[index];

            public IEnumerator<InvocationExpressionSyntax> GetEnumerator() => this.invocations.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.invocations).GetEnumerator();

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                base.VisitInvocationExpression(node);
                var symbol = this.SemanticModel.GetSymbolSafe(node, this.CancellationToken) as IMethodSymbol;
                if (symbol == KnownSymbol.IDisposable.Dispose &&
                    symbol?.Parameters.Length == 0)
                {
                    this.invocations.Add(node);
                }
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                this.identifiers.Add(node);
                base.VisitIdentifierName(node);
            }

            internal static Pool<DisposeWalker>.Pooled Create(ITypeSymbol type, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (!IsAssignableTo(type))
                {
                    return Create(semanticModel, cancellationToken);
                }

                if (TryGetDisposeMethod(type, Search.Recursive, out IMethodSymbol disposeMethod))
                {
                    return Create(disposeMethod, semanticModel, cancellationToken);
                }

                return Create(semanticModel, cancellationToken);
            }

            internal static Pool<DisposeWalker>.Pooled Create(IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (disposeMethod != KnownSymbol.IDisposable.Dispose)
                {
                    return Create(semanticModel, cancellationToken);
                }

                var pooled = Create(semanticModel, cancellationToken);
                foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
                {
                    pooled.Item.Visit(reference.GetSyntax(cancellationToken));
                }

                return pooled;
            }

            internal Result IsMemberDisposed(ISymbol member)
            {
                foreach (var invocation in this.invocations)
                {
                    if (TryGetDisposedRootMember(invocation, this.SemanticModel, this.CancellationToken, out ExpressionSyntax disposed) &&
                        SymbolComparer.Equals(member, this.SemanticModel.GetSymbolSafe(disposed, this.CancellationToken)))
                    {
                        return Result.Yes;
                    }
                }

                foreach (var name in this.identifiers)
                {
                    if (SymbolComparer.Equals(member, this.SemanticModel.GetSymbolSafe(name, this.CancellationToken)))
                    {
                        return Result.Maybe;
                    }
                }

                return Result.No;
            }

            private static Pool<DisposeWalker>.Pooled Create(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var pooled = Pool.GetOrCreate();
                pooled.Item.SemanticModel = semanticModel;
                pooled.Item.CancellationToken = cancellationToken;
                return pooled;
            }
        }
    }
}