namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class FinalizerContextWalker : ExecutionWalker<FinalizerContextWalker>
    {
        private readonly List<SyntaxNode> usedReferenceTypes = new List<SyntaxNode>();
        private readonly List<Recursive> recursive = new List<Recursive>();

        private FinalizerContextWalker()
        {
        }

        /// <summary>
        /// Gets the <see cref="IdentifierNameSyntax"/>s found in the scope.
        /// </summary>
        internal IReadOnlyList<SyntaxNode> UsedReferenceTypes => this.usedReferenceTypes;

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (!IsDisposeBool(node))
            {
                base.VisitInvocationExpression(node);
            }

            static bool IsDisposeBool(InvocationExpressionSyntax candidate)
            {
                return candidate.TryGetMethodName(out var name) &&
                        name == "Dispose" &&
                        candidate.ArgumentList is { } argumentList &&
                        argumentList.Arguments.TrySingle(out _);
            }
        }

        /// <inheritdoc />
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (!IsAssignedNull(node) &&
                this.SemanticModel.TryGetType(node, this.CancellationToken, out var type) &&
                type.IsReferenceType &&
                type.TypeKind != TypeKind.Error)
            {
                this.usedReferenceTypes.Add(node);
            }

            base.VisitIdentifierName(node);
        }

        /// <summary>
        /// Get a walker that has visited <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="semanticModel">The <see cref="SemanticModel"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A walker that has visited <paramref name="node"/>.</returns>
        internal static FinalizerContextWalker Borrow(BaseMethodDeclarationSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = BorrowAndVisit(node, SearchScope.Recursive, semanticModel, cancellationToken, () => new FinalizerContextWalker());
            if (node is MethodDeclarationSyntax)
            {
                walker.usedReferenceTypes.RemoveAll(x => IsInIfDisposing(x));
                walker.recursive.RemoveAll(x => IsInIfDisposing(x.Node));
            }

            foreach (var item in walker.recursive)
            {
                using (var recursiveWalker = RecursiveWalker.Borrow(item.Target, semanticModel, cancellationToken))
                {
                    if (recursiveWalker.UsedReferenceTypes.Count > 0)
                    {
                        walker.usedReferenceTypes.Add(item.Node);
                    }
                }
            }

            return walker;
        }

        /// <inheritdoc />
        protected override void Clear()
        {
            this.usedReferenceTypes.Clear();
            this.recursive.Clear();
            base.Clear();
        }

        protected override bool TryGetTargetSymbol<TSource, TSymbol, TDeclaration>(TSource node, out Target<TSource, TSymbol, TDeclaration> sad)
        {
            if (base.TryGetTargetSymbol(node, out sad))
            {
                this.recursive.Add(new Recursive(node, Target.Create<SyntaxNode,ISymbol, SyntaxNode>(node, sad.Symbol, sad.TargetNode)));
            }

            return false;
        }

        private static bool IsAssignedNull(SyntaxNode node)
        {
            if (node.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression?.IsKind(SyntaxKind.ThisExpression) == true)
            {
                return IsAssignedNull(memberAccess);
            }

            if (node.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Left.Contains(node) &&
                assignment.Right?.IsKind(SyntaxKind.NullLiteralExpression) == true)
            {
                return true;
            }

            return false;
        }

        private static bool IsInIfDisposing(SyntaxNode node)
        {
            if (node.TryFirstAncestor(out IfStatementSyntax? ifStatement))
            {
                if (ifStatement.Statement.Contains(node) &&
                    ifStatement.Condition is IdentifierNameSyntax identifierName &&
                    ifStatement.TryFirstAncestor(out MethodDeclarationSyntax? methodDeclaration) &&
                    methodDeclaration.TryFindParameter(identifierName.Identifier.Text, out _))
                {
                    return true;
                }

                return IsInIfDisposing(ifStatement);
            }

            return false;
        }

        private struct Recursive
        {
            internal readonly SyntaxNode Node;
            internal readonly Target<SyntaxNode, ISymbol, SyntaxNode> Target;

            internal Recursive(SyntaxNode node, Target<SyntaxNode, ISymbol, SyntaxNode> target)
            {
                this.Node = node;
                this.Target = target;
            }
        }

        private sealed class RecursiveWalker : ExecutionWalker<RecursiveWalker>
        {
            private readonly List<SyntaxNode> usedReferenceTypes = new List<SyntaxNode>();

            private RecursiveWalker()
            {
            }

            /// <summary>
            /// Gets the <see cref="IdentifierNameSyntax"/>s found in the scope.
            /// </summary>
            internal IReadOnlyList<SyntaxNode> UsedReferenceTypes => this.usedReferenceTypes;

            /// <inheritdoc />
            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (!IsAssignedNull(node) &&
                    this.SemanticModel.TryGetType(node, this.CancellationToken, out var type) &&
                    type.IsReferenceType &&
                    type.TypeKind != TypeKind.Error)
                {
                    this.usedReferenceTypes.Add(node);
                }

                base.VisitIdentifierName(node);
            }

            internal static RecursiveWalker Borrow(Target<SyntaxNode, ISymbol, SyntaxNode> target, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return BorrowAndVisit(target.TargetNode!, SearchScope.Recursive, semanticModel, cancellationToken, () => new RecursiveWalker());
            }

            /// <inheritdoc />
            protected override void Clear()
            {
                this.usedReferenceTypes.Clear();
                base.Clear();
            }
        }
    }
}
