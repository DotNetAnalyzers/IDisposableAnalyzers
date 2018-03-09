namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Helper for figuring out if the code uses underscore prefix in field names.
    /// </summary>
    internal static class CodeStyle
    {
        private enum Result
        {
            Unknown,
            Yes,
            No,
            Maybe
        }

        /// <summary>
        /// Figuring out if the code uses underscore prefix in field names.
        /// </summary>
        /// <param name="semanticModel">The <see cref="SemanticModel"/></param>
        /// <returns>True if the code is found to prefix field names with underscore.</returns>
        internal static bool UnderscoreFields(this SemanticModel semanticModel)
        {
            using (var walker = FieldWalker.Borrow())
            {
                switch (UnderscoreFields(semanticModel.SyntaxTree, walker))
                {
                    case Result.Unknown:
                    case Result.Maybe:
                        break;
                    case Result.Yes:
                        return true;
                    case Result.No:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var tree in semanticModel.Compilation.SyntaxTrees)
                {
                    switch (UnderscoreFields(tree, walker))
                    {
                        case Result.Unknown:
                        case Result.Maybe:
                            break;
                        case Result.Yes:
                            return true;
                        case Result.No:
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Figuring out if the code uses using directives inside namespaces.
        /// </summary>
        /// <param name="semanticModel">The <see cref="SemanticModel"/></param>
        /// <returns>True if the code is found to prefix field names with underscore.</returns>
        internal static bool UsingDirectivesInsideNamespace(SemanticModel semanticModel)
        {
            using (var walker = UsingDirectiveWalker.Borrow())
            {
                switch (UsingDirectivesInsideNamespace(semanticModel.SyntaxTree, walker))
                {
                    case Result.Unknown:
                    case Result.Maybe:
                        break;
                    case Result.Yes:
                        return true;
                    case Result.No:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var tree in semanticModel.Compilation.SyntaxTrees)
                {
                    switch (UsingDirectivesInsideNamespace(tree, walker))
                    {
                        case Result.Unknown:
                        case Result.Maybe:
                            break;
                        case Result.Yes:
                            return true;
                        case Result.No:
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return true;
        }

        private static Result UnderscoreFields(this SyntaxTree tree, FieldWalker walker)
        {
            if (IsExcluded(tree))
            {
                return Result.Unknown;
            }

            if (tree.TryGetRoot(out var root))
            {
                walker.Visit(root);
                if (walker.UsesThis == Result.Yes ||
                    walker.UsesUnderScore == Result.No)
                {
                    return Result.No;
                }

                if (walker.UsesUnderScore == Result.Yes ||
                    walker.UsesThis == Result.No)
                {
                    return Result.Yes;
                }
            }

            return Result.Unknown;
        }

        private static Result UsingDirectivesInsideNamespace(this SyntaxTree tree, UsingDirectiveWalker walker)
        {
            if (IsExcluded(tree))
            {
                return Result.Unknown;
            }

            if (tree.TryGetRoot(out var root))
            {
                walker.Visit(root);
            }

            return walker.UsingDirectivesInside();
        }

        private static bool IsExcluded(SyntaxTree syntaxTree)
        {
            return syntaxTree.FilePath.EndsWith(".g.i.cs") ||
                   syntaxTree.FilePath.EndsWith(".g.cs");
        }

        private sealed class FieldWalker : PooledWalker<FieldWalker>
        {
            private FieldWalker()
            {
            }

            public Result UsesThis { get; private set; }

            public Result UsesUnderScore { get; private set; }

            public static FieldWalker Borrow() => Borrow(() => new FieldWalker());

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.IsMissing ||
                    node.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                    node.Modifiers.Any(SyntaxKind.ConstKeyword) ||
                    node.Modifiers.Any(SyntaxKind.PublicKeyword) ||
                    node.Modifiers.Any(SyntaxKind.ProtectedKeyword) ||
                    node.Modifiers.Any(SyntaxKind.InternalKeyword))
                {
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
            }

            public override void VisitThisExpression(ThisExpressionSyntax node)
            {
                switch (node.Parent.Kind())
                {
                    case SyntaxKind.Argument:
                        return;
                }

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
            }

            private void CheckUsesThis(ExpressionSyntax expression)
            {
                if (expression == null ||
                    this.UsesThis != Result.Unknown)
                {
                    return;
                }

                if (expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is ThisExpressionSyntax)
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

                if (expression is IdentifierNameSyntax identifierName &&
                    expression.FirstAncestor<TypeDeclarationSyntax>() is TypeDeclarationSyntax typeDeclaration)
                {
                    if (typeDeclaration.TryFindField(identifierName.Identifier.ValueText, out var field) &&
                        (field.Modifiers.Any(SyntaxKind.StaticKeyword) || field.Modifiers.Any(SyntaxKind.ConstKeyword)))
                    {
                        return;
                    }

                    if (typeDeclaration.TryFindProperty(identifierName.Identifier.ValueText, out var property) &&
                        property.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        return;
                    }

                    if (typeDeclaration.TryFindMethod(identifierName.Identifier.ValueText, out var method) &&
                        method.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        return;
                    }

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

        private sealed class UsingDirectiveWalker : PooledWalker<UsingDirectiveWalker>
        {
            private readonly List<UsingDirectiveSyntax> usingDirectives = new List<UsingDirectiveSyntax>();

            public static UsingDirectiveWalker Borrow() => Borrow(() => new UsingDirectiveWalker());

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                this.usingDirectives.Add(node);
                base.VisitUsingDirective(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                // Stop walking here
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                // Stop walking here
            }

            internal Result UsingDirectivesInside()
            {
                if (this.usingDirectives.Count == 0)
                {
                    return Result.Unknown;
                }

                if (this.usingDirectives.TryFirst(x => x.FirstAncestor<NamespaceDeclarationSyntax>() != null, out _))
                {
                    return this.usingDirectives.TryFirst(x => x.FirstAncestor<NamespaceDeclarationSyntax>() == null, out _)
                        ? Result.Maybe
                        : Result.Yes;
                }

                return Result.No;
            }

            protected override void Clear()
            {
                this.usingDirectives.Clear();
            }
        }
    }
}
