namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
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

            using (var pooled = DisposeWalker.Borrow(context, semanticModel, cancellationToken))
            {
                return pooled.IsMemberDisposed(member);
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
                using (var pooled = DisposeWalker.Borrow(disposeMethod, semanticModel, cancellationToken))
                {
                    foreach (var invocation in pooled)
                    {
                        if (IsDisposing(invocation, member, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }

                using (var walker = IdentifierNameWalker.Borrow(node))
                {
                    foreach (var identifier in walker.IdentifierNames)
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
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is IdentifierNameSyntax methodName &&
                methodName.Identifier.ValueText != "Dispose")
            {
                return false;
            }

            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method &&
                method.Parameters.Length == 0 &&
                method == KnownSymbol.IDisposable.Dispose &&
                TryGetDisposedRootMember(invocation, semanticModel, cancellationToken, out var disposed))
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
                    using (var pooled = ReturnValueWalker.Borrow(node, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                    {
                        if (pooled.Count == 0)
                        {
                            return false;
                        }

                        return pooled.TrySingle(out var expression) &&
                               MemberPath.TryFindRootMember(expression, out disposedMember);
                    }
                }
            }

            return false;
        }

        internal static bool IsDisposedBefore(ISymbol symbol, ExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            bool IsDisposing(InvocationExpressionSyntax invocation, ISymbol current)
            {
                if (invocation.TryGetMethodName(out var name) &&
                    name != "Dispose")
                {
                    return false;
                }

                var invokedSymbol = semanticModel.GetSymbolSafe(invocation, cancellationToken);
                if (invokedSymbol?.Name != "Dispose")
                {
                    return false;
                }

                var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
                if (statement != null)
                {
                    using (var names = IdentifierNameWalker.Borrow(statement))
                    {
                        foreach (var identifierName in names.IdentifierNames)
                        {
                            if (identifierName.Identifier.ValueText == current.Name &&
                                SymbolComparer.Equals(current, semanticModel.GetSymbolSafe(identifierName, cancellationToken)))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            bool TryGetScope(SyntaxNode node, out BlockSyntax result)
            {
                result = null;
                if (node.FirstAncestor<AnonymousFunctionExpressionSyntax>() is AnonymousFunctionExpressionSyntax lambda)
                {
                    result = lambda.Body as BlockSyntax;
                }
                else if (node.FirstAncestor<AccessorDeclarationSyntax>() is AccessorDeclarationSyntax accessor)
                {
                    result = accessor.Body;
                }
                else if (node.FirstAncestor<BaseMethodDeclarationSyntax>() is BaseMethodDeclarationSyntax method)
                {
                    result = method.Body;
                }

                return result != null;
            }

            if (TryGetScope(assignment, out var block))
            {
                using (var walker = InvocationWalker.Borrow(block))
                {
                    foreach (var invocation in walker.Invocations)
                    {
                        if (invocation.IsExecutedBefore(assignment) != Result.Yes)
                        {
                            continue;
                        }

                        if (IsDisposing(invocation, symbol))
                        {
                            return true;
                        }
                    }
                }
            }

            if (assignment is AssignmentExpressionSyntax assignmentExpression &&
                semanticModel.GetSymbolSafe(assignmentExpression.Left, cancellationToken) is IPropertySymbol property &&
                property.TryGetSetter(cancellationToken, out var setter))
            {
                using (var pooled = InvocationWalker.Borrow(setter))
                {
                    foreach (var invocation in pooled.Invocations)
                    {
                        if (IsDisposing(invocation, symbol) ||
                            IsDisposing(invocation, property))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal sealed class DisposeWalker : ExecutionWalker<DisposeWalker>, IReadOnlyList<InvocationExpressionSyntax>
        {
            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
            private readonly List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();

            private DisposeWalker()
            {
                this.Search = ReturnValueSearch.Recursive;
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
                if (!IsAssignableFrom(type, semanticModel.Compilation))
                {
                    return Borrow(semanticModel, cancellationToken);
                }

                if (TryGetDisposeMethod(type, semanticModel.Compilation, Gu.Roslyn.AnalyzerExtensions.Search.Recursive, out var disposeMethod))
                {
                    return Borrow(disposeMethod, semanticModel, cancellationToken);
                }

                return Borrow(semanticModel, cancellationToken);
            }

            internal static DisposeWalker Borrow(IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (disposeMethod != KnownSymbol.IDisposable.Dispose)
                {
                    return Borrow(semanticModel, cancellationToken);
                }

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
                    if (invocation.TryGetMethodName(out var name) &&
                        name != "Dispose")
                    {
                        continue;
                    }

                    if (TryGetDisposedRootMember(invocation, this.SemanticModel, this.CancellationToken, out var disposed) &&
                        SymbolComparer.Equals(member, this.SemanticModel.GetSymbolSafe(disposed, this.CancellationToken)))
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
}
