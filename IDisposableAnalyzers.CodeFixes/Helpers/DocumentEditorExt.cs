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
                accessibility: accessibility,
                modifiers: modifiers,
                type: type);
            editor.AddField(containingType, newField);
            return newField;
        }

        internal static void AddField(this DocumentEditor editor, TypeDeclarationSyntax containingType, FieldDeclarationSyntax field)
        {
            FieldDeclarationSyntax existing = null;
            foreach (var member in containingType.Members)
            {
                if (member is FieldDeclarationSyntax candidate)
                {
                    if (field.IsInsertBefore(candidate))
                    {
                        editor.InsertBefore(candidate, field);
                        return;
                    }

                    existing = candidate;
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

        internal static void AddMethod(this DocumentEditor editor, TypeDeclarationSyntax containingType, MethodDeclarationSyntax method)
        {
            MethodDeclarationSyntax existing = null;
            foreach (var member in containingType.Members)
            {
                if (member is MethodDeclarationSyntax candidate)
                {
                    if (method.IsInsertBefore(candidate))
                    {
                        editor.InsertBefore(candidate, method);
                        return;
                    }

                    existing = candidate;
                }
            }

            if (existing != null)
            {
                editor.InsertAfter(existing, method);
            }
            else
            {
                editor.AddMember(containingType, method);
            }
        }

        internal static DocumentEditor MakeSealed(this DocumentEditor editor, TypeDeclarationSyntax type)
        {
            if (type.Modifiers.Any())
            {
                editor.ReplaceNode(
                    type,
                    type.WithModifiers(type.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword))));
            }
            else
            {
                editor.SetModifiers(type, DeclarationModifiers.Sealed);
            }

            foreach (var member in type.Members)
            {
                var modifiers = member.Modifiers();
                if (modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out SyntaxToken modifier))
                {
                    editor.ReplaceNode(
                        member,
                        member.WithModifiers(modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword))));
                }

                if (modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                {
                    editor.ReplaceNode(member, member.WithModifiers(modifiers.Remove(modifier)));
                }

                if (member is BasePropertyDeclarationSyntax prop &&
                    prop.AccessorList != null)
                {
                    foreach (var accessor in prop.AccessorList.Accessors)
                    {
                        modifiers = accessor.Modifiers;
                        if (modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                        {
                            editor.ReplaceNode(
                                accessor,
                                accessor.WithModifiers(modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword))));
                        }

                        if (modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                        {
                            editor.ReplaceNode(accessor, accessor.WithModifiers(modifiers.Remove(modifier)));
                        }
                    }
                }
            }

            return editor;
        }

        private static bool IsInsertBefore(this FieldDeclarationSyntax toAdd, FieldDeclarationSyntax existing)
        {
            return Index(existing.Modifiers) > Index(toAdd.Modifiers);
        }

        private static bool IsInsertBefore(this MethodDeclarationSyntax toAdd, MethodDeclarationSyntax existing)
        {
            return Index(existing.Modifiers) > Index(toAdd.Modifiers);
        }

        private static SyntaxTokenList Modifiers(this MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    return field.Modifiers;
                case BasePropertyDeclarationSyntax prop:
                    return prop.Modifiers;
                case BaseMethodDeclarationSyntax method:
                    return method.Modifiers;
                case TypeDeclarationSyntax type:
                    return type.Modifiers;
                default:
                    return default(SyntaxTokenList);
            }
        }

        private static MemberDeclarationSyntax WithModifiers(this MemberDeclarationSyntax member, SyntaxTokenList modifiers)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    return field.WithModifiers(modifiers);
                case ConstructorDeclarationSyntax ctor:
                    return ctor.WithModifiers(modifiers);
                case EventDeclarationSyntax @event:
                    return @event.WithModifiers(modifiers);
                case PropertyDeclarationSyntax prop:
                    return prop.WithModifiers(modifiers);
                case MethodDeclarationSyntax method:
                    return method.WithModifiers(modifiers);
                case ClassDeclarationSyntax type:
                    return type.WithModifiers(modifiers);
                default:
                    return member;
            }
        }

        private static int Index(SyntaxTokenList modifiers)
        {
            int SubIndex(SyntaxTokenList ms, int i)
            {
                if (modifiers.Any(SyntaxKind.ConstKeyword))
                {
                    return i;
                }

                if (modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    if (modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                    {
                        return i + 1;
                    }

                    return i + 2;
                }

                return i + 3;
            }

            if (modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return SubIndex(modifiers, 0);
            }

            if (modifiers.Any(SyntaxKind.InternalKeyword))
            {
                return SubIndex(modifiers, 4);
            }

            if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                return SubIndex(modifiers, 8);
            }

            if (modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return SubIndex(modifiers, 12);
            }

            return SubIndex(modifiers, 0);
        }
    }
}
