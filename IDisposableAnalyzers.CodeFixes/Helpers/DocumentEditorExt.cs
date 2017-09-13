namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class DocumentEditorExt
    {
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
