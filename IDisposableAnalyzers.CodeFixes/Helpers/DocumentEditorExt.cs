namespace IDisposableAnalyzers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class DocumentEditorExt
    {
        internal static FieldDeclarationSyntax AddField(this DocumentEditor editor, string name, TypeDeclarationSyntax containingType, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, CancellationToken cancellationToken)
        {
            return AddField(
                editor,
                name,
                containingType,
                accessibility,
                modifiers,
                SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)),
                cancellationToken);
        }

        internal static FieldDeclarationSyntax AddField(this DocumentEditor editor, string name, TypeDeclarationSyntax containingType, Accessibility accessibility, DeclarationModifiers modifiers, TypeSyntax type, CancellationToken cancellationToken)
        {
            var declaredSymbol = (INamedTypeSymbol)editor.SemanticModel.GetDeclaredSymbolSafe(containingType, cancellationToken);
            while (declaredSymbol.MemberNames.Contains(name))
            {
                name += "_";
            }

            var newField = (FieldDeclarationSyntax)editor.Generator.FieldDeclaration(
                name,
                accessibility: Accessibility.Private,
                modifiers: DeclarationModifiers.ReadOnly,
                type: type);
            editor.AddField(containingType, newField);
            return newField;
        }

        internal static void AddField(this DocumentEditor editor, TypeDeclarationSyntax containingType, FieldDeclarationSyntax field)
        {
            FieldDeclarationSyntax existing = null;
            foreach (var member in containingType.Members)
            {
                if (member is FieldDeclarationSyntax fieldDeclaration)
                {
                    if (IsInsertBefore(fieldDeclaration))
                    {
                        editor.InsertBefore(fieldDeclaration, field);
                        return;
                    }

                    existing = fieldDeclaration;
                    continue;
                }

                editor.InsertBefore(member, field);
                return;
            }

            if (existing != null)
            {
                editor.InsertAfter(existing, field);
            }
            else
            {
                editor.AddMember(containingType, field);
            }
        }

        private static bool IsInsertBefore(FieldDeclarationSyntax existing)
        {
            if (!existing.Modifiers.Any(SyntaxKind.PrivateKeyword) ||
                 existing.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                 existing.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return false;
            }

            return true;
        }
    }
}
