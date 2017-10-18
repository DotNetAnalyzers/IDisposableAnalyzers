namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Names
    {
        internal static bool UsesUnderscoreNames(this SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = Walker.Borrow(node, semanticModel, cancellationToken))
            {
                if (walker.UsesThis == Result.Yes ||
                    walker.UsesUnderScore == Result.No)
                {
                    return false;
                }

                if (walker.UsesUnderScore == Result.Yes ||
                    walker.UsesThis == Result.No)
                {
                    return true;
                }

                foreach (var tree in semanticModel.Compilation.SyntaxTrees)
                {
                    if (tree.FilePath.EndsWith(".g.i.cs"))
                    {
                        continue;
                    }

                    walker.Visit(tree.GetRoot(cancellationToken));
                    if (walker.UsesThis == Result.Yes ||
                        walker.UsesUnderScore == Result.No)
                    {
                        return false;
                    }

                    if (walker.UsesUnderScore == Result.Yes ||
                        walker.UsesThis == Result.No)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal sealed class Walker : PooledWalker<Walker>
        {
            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private Walker()
            {
            }

            public Result UsesThis { get; private set; }

            public Result UsesUnderScore { get; private set; }

            public static Walker Borrow(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var walker = Borrow(() => new Walker());
                walker.semanticModel = semanticModel;
                walker.cancellationToken = cancellationToken;
                while (node.Parent != null)
                {
                    node = node.Parent;
                }

                walker.Visit(node);
                return walker;
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.IsMissing ||
                    node.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                    node.Modifiers.Any(SyntaxKind.ConstKeyword) ||
                    node.Modifiers.Any(SyntaxKind.PublicKeyword) ||
                    node.Modifiers.Any(SyntaxKind.ProtectedKeyword) ||
                    node.Modifiers.Any(SyntaxKind.InternalKeyword))
                {
                    base.VisitFieldDeclaration(node);
                    return;
                }

                foreach (var variable in node.Declaration.Variables)
                {
                    var name = variable.Identifier.ValueText;
                    if (name.StartsWith("_"))
                    {
                        switch (this.UsesUnderScore)
                        {
                            case Result.Unknown:
                                this.UsesUnderScore = Result.Yes;
                                break;
                            case Result.Yes:
                                break;
                            case Result.No:
                                this.UsesUnderScore = Result.Maybe;
                                break;
                            case Result.Maybe:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else
                    {
                        switch (this.UsesUnderScore)
                        {
                            case Result.Unknown:
                                this.UsesUnderScore = Result.No;
                                break;
                            case Result.Yes:
                                this.UsesUnderScore = Result.Maybe;
                                break;
                            case Result.No:
                                break;
                            case Result.Maybe:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                base.VisitFieldDeclaration(node);
            }

            public override void VisitThisExpression(ThisExpressionSyntax node)
            {
                switch (this.UsesThis)
                {
                    case Result.Unknown:
                        this.UsesThis = Result.Yes;
                        break;
                    case Result.Yes:
                        break;
                    case Result.No:
                        this.UsesThis = Result.Maybe;
                        break;
                    case Result.Maybe:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                base.VisitThisExpression(node);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                this.CheckUsesThis(node.Left);
                base.VisitAssignmentExpression(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                this.CheckUsesThis(node.Expression);
                base.VisitInvocationExpression(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                this.CheckUsesThis(node);
                base.VisitMemberAccessExpression(node);
            }

            public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            {
                this.CheckUsesThis(node.Expression);
                base.VisitConditionalAccessExpression(node);
            }

            protected override void Clear()
            {
                this.UsesThis = Result.Unknown;
                this.UsesUnderScore = Result.Unknown;
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
            }

            private void CheckUsesThis(ExpressionSyntax expression)
            {
                if (expression == null)
                {
                    return;
                }

                if ((expression as MemberAccessExpressionSyntax)?.Expression is ThisExpressionSyntax)
                {
                    switch (this.UsesThis)
                    {
                        case Result.Unknown:
                            this.UsesThis = Result.Yes;
                            break;
                        case Result.Yes:
                            break;
                        case Result.No:
                            this.UsesThis = Result.Maybe;
                            break;
                        case Result.Maybe:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (expression is IdentifierNameSyntax)
                {
                    if (this.semanticModel.GetSymbolSafe(expression, this.cancellationToken)
                            ?.IsStatic ==
                        false)
                    {
                        switch (this.UsesThis)
                        {
                            case Result.Unknown:
                                this.UsesThis = Result.No;
                                break;
                            case Result.Yes:
                                this.UsesThis = Result.Maybe;
                                break;
                            case Result.No:
                                break;
                            case Result.Maybe:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }
    }
}