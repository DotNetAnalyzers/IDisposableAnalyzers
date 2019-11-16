namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class DocumentEditorExt
    {
        private static readonly UsingDirectiveSyntax UsingSystem = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"));

        internal static Task<ExpressionSyntax> AddFieldAsync(this DocumentEditor editor, TypeDeclarationSyntax containingType, string name, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, CancellationToken cancellationToken)
        {
            return AddFieldAsync(
                editor,
                containingType,
                name,
                accessibility,
                modifiers,
                (TypeSyntax)editor.Generator.TypeExpression(type),
                cancellationToken);
        }

        internal static async Task<ExpressionSyntax> AddFieldAsync(this DocumentEditor editor, TypeDeclarationSyntax containingType, string name, Accessibility accessibility, DeclarationModifiers modifiers, TypeSyntax type, CancellationToken cancellationToken)
        {
            if (!name.StartsWith("_", StringComparison.Ordinal) &&
                await editor.OriginalDocument.UnderscoreFieldsAsync(cancellationToken).ConfigureAwait(false) == CodeStyleResult.Yes)
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
            switch (await editor.OriginalDocument.QualifyFieldAccessAsync(cancellationToken).ConfigureAwait(false))
            {
                case CodeStyleResult.NotFound
                    when name.StartsWith("_", StringComparison.Ordinal):
                    return identifierNameSyntax;
                case CodeStyleResult.Yes:
                case CodeStyleResult.Mixed:
                case CodeStyleResult.NotFound:
                    return SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        identifierNameSyntax);
                case CodeStyleResult.No:
                    return identifierNameSyntax;
                default:
                    throw new InvalidOperationException("Unhandled code style.");
            }
        }

        internal static async Task<ExpressionStatementSyntax> ThisDisposedTrueAsync(this DocumentEditor editor, CancellationToken cancellationToken)
        {
            if (await editor.OriginalDocument.QualifyMethodAccessAsync(cancellationToken).ConfigureAwait(false) == CodeStyleResult.Yes)
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
