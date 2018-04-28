namespace IDisposableAnalyzers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static partial class DocumentEditorExt
    {
        internal static FieldDeclarationSyntax AddField(
            this DocumentEditor editor,
            TypeDeclarationSyntax containingType,
            string name,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            CancellationToken cancellationToken)
        {
            return AddField(
                editor,
                containingType,
                name,
                accessibility,
                modifiers,
                SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)),
                cancellationToken);
        }

        internal static FieldDeclarationSyntax AddField(
            this DocumentEditor editor,
            TypeDeclarationSyntax containingType,
            string name,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            TypeSyntax type,
            CancellationToken cancellationToken)
        {
            var declaredSymbol = (INamedTypeSymbol)editor.SemanticModel.GetDeclaredSymbolSafe(containingType, cancellationToken);
            while (declaredSymbol.MemberNames.Contains(name))
            {
                name += "_";
            }

            var newField = (FieldDeclarationSyntax)editor.Generator.FieldDeclaration(
                name,
                accessibility: accessibility,
                modifiers: modifiers,
                type: type);
            editor.AddField(containingType, newField);
            return newField;
        }
    }
}
