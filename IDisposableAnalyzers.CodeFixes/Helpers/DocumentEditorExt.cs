namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class DocumentEditorExt
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

        internal static void AddField(this DocumentEditor editor, TypeDeclarationSyntax containingType, FieldDeclarationSyntax field)
        {
            editor.ReplaceNode(containingType, (node, generator) => AddSorted(generator, (TypeDeclarationSyntax)node, field));
        }

        internal static void AddMethod(this DocumentEditor editor, TypeDeclarationSyntax containingType, MethodDeclarationSyntax method)
        {
            editor.ReplaceNode(containingType, (node, generator) => AddSorted(generator, (TypeDeclarationSyntax)node, method));
        }

        internal static DocumentEditor MakeSealed(this DocumentEditor editor, ClassDeclarationSyntax type)
        {
            editor.SetModifiers(type, DeclarationModifiers.From(editor.SemanticModel.GetDeclaredSymbol(type)).WithIsSealed(isSealed: true));
            foreach (var member in type.Members)
            {
                var modifiers = member.Modifiers();
                if (modifiers.Any(SyntaxKind.ProtectedKeyword))
                {
                    editor.SetAccessibility(member, Accessibility.Private);
                }

                if (modifiers.Any(SyntaxKind.VirtualKeyword))
                {
                    editor.SetModifiers(member, DeclarationModifiers.None);
                }

                if (member is BasePropertyDeclarationSyntax prop &&
                    prop.AccessorList != null)
                {
                    foreach (var accessor in prop.AccessorList.Accessors)
                    {
                        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
                        {
                            editor.SetModifiers(accessor, DeclarationModifiers.None);
                        }

                        if (accessor.Modifiers.Any(SyntaxKind.ProtectedKeyword))
                        {
                            editor.SetAccessibility(accessor, Accessibility.Private);
                        }
                    }
                }
            }

            return editor;
        }

        private static SyntaxNode AddSorted(SyntaxGenerator generator, TypeDeclarationSyntax containingType, MemberDeclarationSyntax memberDeclaration)
        {
            var memberIndex = MemberIndex(memberDeclaration);
            for (var i = 0; i < containingType.Members.Count; i++)
            {
                var member = containingType.Members[i];
                if (memberIndex < MemberIndex(member))
                {
                    return generator.InsertMembers(containingType, i, memberDeclaration);
                }
            }

            return generator.AddMembers(containingType, memberDeclaration);
        }

        private static int MemberIndex(MemberDeclarationSyntax member)
        {
            int ModifierOffset(SyntaxTokenList modifiers)
            {
                if (modifiers.Any(SyntaxKind.ConstKeyword))
                {
                    return 0;
                }

                if (modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    if (modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                    {
                        return 1;
                    }

                    return 2;
                }

                if (modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                {
                    return 3;
                }

                return 4;
            }

            int AccessOffset(Accessibility accessibility)
            {
                const int step = 5;
                switch (accessibility)
                {
                    case Accessibility.Public:
                        return 0 * step;
                    case Accessibility.Internal:
                        return 1 * step;
                    case Accessibility.ProtectedAndInternal:
                        return 2 * step;
                    case Accessibility.ProtectedOrInternal:
                        return 3 * step;
                    case Accessibility.Protected:
                        return 4 * step;
                    case Accessibility.Private:
                        return 5 * step;
                    case Accessibility.NotApplicable:
                        return int.MinValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null);
                }
            }

            Accessibility Accessability(SyntaxTokenList modifiers)
            {
                if (modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    return Accessibility.Public;
                }

                if (modifiers.Any(SyntaxKind.InternalKeyword))
                {
                    return Accessibility.Public;
                }

                if (modifiers.Any(SyntaxKind.ProtectedKeyword))
                {
                    return Accessibility.Protected;
                }

                if (modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    return Accessibility.Private;
                }

                return Accessibility.Private;
            }

            int TypeOffset(SyntaxKind kind)
            {
                const int step = 5 * 6;
                switch (kind)
                {
                    case SyntaxKind.FieldDeclaration:
                        return 0 * step;
                    case SyntaxKind.ConstructorDeclaration:
                        return 1 * step;
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                        return 2 * step;
                    case SyntaxKind.PropertyDeclaration:
                        return 3 * step;
                    case SyntaxKind.MethodDeclaration:
                        return 4 * step;
                    default:
                        return int.MinValue;
                }
            }

            var mfs = member.Modifiers();
            return TypeOffset(member.Kind()) + AccessOffset(Accessability(mfs)) + ModifierOffset(mfs);
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
    }
}
