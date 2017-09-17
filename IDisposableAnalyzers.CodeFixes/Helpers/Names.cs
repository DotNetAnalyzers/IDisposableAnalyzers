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
            using (var pooled = Walker.Create(node, semanticModel, cancellationToken))
            {
                if (pooled.Item.UsesThis == Result.Yes)
                {
                    return false;
                }

                if (pooled.Item.UsesUnderScore == Result.Yes)
                {
                    return true;
                }
            }

            foreach (var tree in semanticModel.Compilation.SyntaxTrees)
            {
                using (var pooled = Walker.Create(tree.GetRoot(cancellationToken), semanticModel, cancellationToken))
                {
                    if (pooled.Item.UsesThis == Result.Yes)
                    {
                        return false;
                    }

                    if (pooled.Item.UsesUnderScore == Result.Yes)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal sealed class Walker : CSharpSyntaxWalker
        {
            private static readonly Pool<Walker> Cache = new Pool<Walker>(
                () => new Walker(),
                x =>
                {
                    x.UsesThis = Result.Unknown;
                    x.UsesUnderScore = Result.Unknown;
                    x.semanticModel = null;
                    x.cancellationToken = CancellationToken.None;
                });

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private Walker()
            {
            }

            public Result UsesThis { get; private set; }

            public Result UsesUnderScore { get; private set; }

            public static Pool<Walker>.Pooled Create(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var pooled = Cache.GetOrCreate();
                while (node.Parent != null)
                {
                    node = node.Parent;
                }

                pooled.Item.semanticModel = semanticModel;
                pooled.Item.cancellationToken = cancellationToken;
                pooled.Item.Visit(node);
                return pooled;
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
                    if (this.semanticModel.GetSymbolSafe(expression, this.cancellationToken)?.IsStatic == false)
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