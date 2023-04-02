namespace IDisposableAnalyzers;

using System.Collections.Generic;
using System.Threading;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed class FinalizerContextWalker : RecursiveWalker<FinalizerContextWalker>
{
    private readonly List<SyntaxNode> usedReferenceTypes = new();
    private bool returned;

    private FinalizerContextWalker()
    {
    }

    internal IReadOnlyList<SyntaxNode> UsedReferenceTypes => this.usedReferenceTypes;

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        if (IsParameter(node.Condition))
        {
            this.Visit(node.Else);
        }
        else if (node.Condition is PrefixUnaryExpressionSyntax { Operand: IdentifierNameSyntax identifierName, OperatorToken.ValueText: "!" } &&
                 IsParameter(identifierName))
        {
            switch (node.Statement)
            {
                case ReturnStatementSyntax _:
                    this.returned = true;
                    break;
                case BlockSyntax block:
                    foreach (var statement in block.Statements)
                    {
                        if (statement is ReturnStatementSyntax)
                        {
                            this.returned = true;
                            return;
                        }

                        this.Visit(statement);
                    }

                    break;
            }
        }
        else
        {
            base.VisitIfStatement(node);
        }

        bool IsParameter(ExpressionSyntax? expression)
        {
            return expression switch
            {
                IdentifierNameSyntax { Identifier.ValueText: { } name }
                    => node.TryFirstAncestor(out MethodDeclarationSyntax? methodDeclaration) &&
                       methodDeclaration.TryFindParameter(name, out _),
                BinaryExpressionSyntax { Left: { } left, OperatorToken.ValueText: "&&", Right: { } right }
                    => IsParameter(left) || IsParameter(right),
                _ => false,
            };
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
        if (!this.returned &&
            !IsAssignedNull(node) &&
            TypeSymbol() is { IsReferenceType: true, TypeKind: not TypeKind.Error })
        {
            this.usedReferenceTypes.Add(node);
        }

        base.VisitIdentifierName(node);

        ITypeSymbol? TypeSymbol()
        {
            return this.SemanticModel.GetSymbolSafe(node, this.CancellationToken) switch
            {
                IFieldSymbol symbol => symbol.Type,
                IPropertySymbol symbol => symbol.Type,
                ILocalSymbol symbol => symbol.Type,
                IParameterSymbol symbol => symbol.Type,
                ITypeSymbol => null,
                //// Defaulting to returning the type for unhandled cases. This means we risk warning too much.
                _ => this.SemanticModel.GetType(node, this.CancellationToken),
            };
        }
    }

    internal static FinalizerContextWalker Borrow(BaseMethodDeclarationSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var walker = node switch
        {
            { ExpressionBody: { } body }
                => BorrowAndVisit(body, SearchScope.Type, semanticModel, cancellationToken, () => new FinalizerContextWalker()),
            { Body: { } body }
                => BorrowAndVisit(body, SearchScope.Type, semanticModel, cancellationToken, () => new FinalizerContextWalker()),
            _ => Borrow(() => new FinalizerContextWalker()),
        };

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
        this.returned = false;
        base.Clear();
    }

    private static bool IsAssignedNull(SyntaxNode node)
    {
        if (node.Parent is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression.IsKind(SyntaxKind.ThisExpression))
        {
            return IsAssignedNull(memberAccess);
        }

        if (node.Parent is AssignmentExpressionSyntax assignment &&
            assignment.Left.Contains(node) &&
            assignment.Right.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return true;
        }

        return false;
    }

    private sealed class TargetWalker : ExecutionWalker<TargetWalker>
    {
        private readonly List<SyntaxNode> usedReferenceTypes = new();

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
            return BorrowAndVisit(target.Declaration!, SearchScope.Recursive, recursion, () => new TargetWalker());
        }

        protected override void Clear()
        {
            this.usedReferenceTypes.Clear();
            base.Clear();
        }
    }
}
