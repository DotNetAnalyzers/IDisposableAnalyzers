namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static partial class DocumentEditorExt
    {
        private static readonly UsingDirectiveSyntax UsingSystem = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"));

        internal static ExpressionSyntax AddField(
            this DocumentEditor editor,
            TypeDeclarationSyntax containingType,
            string name,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type)
        {
            return AddField(
                editor,
                containingType,
                name,
                accessibility,
                modifiers,
                (TypeSyntax)editor.Generator.TypeExpression(type));
        }

        internal static ExpressionSyntax AddField(
            this DocumentEditor editor,
            TypeDeclarationSyntax containingType,
            string name,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            TypeSyntax type)
        {
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields() == CodeStyleResult.Yes;
            if (usesUnderscoreNames &&
                !name.StartsWith("_", StringComparison.Ordinal))
            {
                name = $"_{name}";
            }

            while (containingType.TryFindField(name, out _))
            {
                name += "_";
            }

            var field = (FieldDeclarationSyntax)editor.Generator.FieldDeclaration(
                name,
                accessibility: accessibility,
                modifiers: modifiers,
                type: type);
            _ = editor.AddField(containingType, field);
            var identifierNameSyntax = SyntaxFactory.IdentifierName(field.Declaration.Variables[0].Identifier);
            return usesUnderscoreNames
                ? (ExpressionSyntax)identifierNameSyntax
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    identifierNameSyntax);
        }

        internal static ExpressionStatementSyntax ThisDisposedTrue(this DocumentEditor editor, CancellationToken cancellationToken)
        {
            if (editor.SemanticModel.UnderscoreFields() == CodeStyleResult.Yes)
            {
                return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("Dispose"),
                        IDisposableFactory.Arguments(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        SyntaxFactory.IdentifierName("Dispose")),
                    IDisposableFactory.Arguments(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))));
        }

        internal static DocumentEditor AddIDisposableInterface(this DocumentEditor editor, TypeDeclarationSyntax type)
        {
            if (IsMissing())
            {
                editor.AddUsing(UsingSystem)
                      .AddInterfaceType(type, IDisposableFactory.SystemIDisposable);
            }

            return editor;

            bool IsMissing()
            {
                return type switch
                {
                    { BaseList: { Types: { } types } } => !types.TryFirst(x => x == KnownSymbol.IDisposable, out _),
                    _ => true,
                };
            }
        }

        internal static DocumentEditor AddThrowIfDisposed(this DocumentEditor editor, TypeDeclarationSyntax containingType, ExpressionSyntax disposed, CancellationToken cancellationToken)
        {
            if (editor.SemanticModel.TryGetSymbol(containingType, cancellationToken, out var type) &&
                !type.TryFindSingleMethodRecursive("ThrowIfDisposed", out _))
            {
                return type.IsSealed
                    ? editor.AddMethod(containingType, MethodFactory.PrivateThrowIfDisposed(disposed))
                    : editor.AddMethod(containingType, MethodFactory.ProtectedThrowIfDisposed(disposed));
            }

            return editor;
        }

        internal static DocumentEditor AddPrivateThrowIfDisposed(this DocumentEditor editor, TypeDeclarationSyntax containingType, ExpressionSyntax disposed, CancellationToken cancellationToken)
        {
            if (editor.SemanticModel.TryGetSymbol(containingType, cancellationToken, out var type) &&
                !type.TryFindSingleMethodRecursive("ThrowIfDisposed", out _))
            {
                return editor.AddMethod(containingType, MethodFactory.PrivateThrowIfDisposed(disposed));
            }

            return editor;
        }
    }
}
