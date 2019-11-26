namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class FinalizerContextWalker : RecursiveWalker<FinalizerContextWalker>
    {
        private readonly List<SyntaxNode> usedReferenceTypes = new List<SyntaxNode>();

        private FinalizerContextWalker()
        {
        }

        internal IReadOnlyList<SyntaxNode> UsedReferenceTypes => this.usedReferenceTypes;

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            if (node.Condition is IdentifierNameSyntax identifierName &&
                node.TryFirstAncestor(out MethodDeclarationSyntax? methodDeclaration) &&
                methodDeclaration.TryFindParameter(identifierName.Identifier.Text, out _))
            {
                this.Visit(node.Else);
            }
            else
            {
                base.VisitIfStatement(node);
            }
        }

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

        internal static FinalizerContextWalker Borrow(BaseMethodDeclarationSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = BorrowAndVisit(node, SearchScope.Type, semanticModel, cancellationToken, () => new FinalizerContextWalker());

            foreach (var target in walker.Targets)
            {
                using var recursiveWalker = TargetWalker.Borrow(target, walker.Recursion);
                if (recursiveWalker.UsedReferenceTypes.Count > 0)
                {
                    walker.usedReferenceTypes.Add(target.Source);
                }
            }

            return walker;
        }

        protected override void Clear()
        {
            this.usedReferenceTypes.Clear();
            base.Clear();
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

        private sealed class TargetWalker : ExecutionWalker<TargetWalker>
        {
            private readonly List<SyntaxNode> usedReferenceTypes = new List<SyntaxNode>();

            private TargetWalker()
            {
            }

            internal IReadOnlyList<SyntaxNode> UsedReferenceTypes => this.usedReferenceTypes;

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

            internal static TargetWalker Borrow(Target<SyntaxNode, ISymbol, SyntaxNode> target, Recursion recursion)
            {
                return BorrowAndVisit(target.TargetNode!, SearchScope.Recursive, recursion, () => new TargetWalker());
            }

            protected override void Clear()
            {
                this.usedReferenceTypes.Clear();
                base.Clear();
            }
        }
    }
}
