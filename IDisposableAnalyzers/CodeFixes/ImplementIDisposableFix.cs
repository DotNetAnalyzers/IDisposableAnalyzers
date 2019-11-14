namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableFix))]
    [Shared]
    internal class ImplementIDisposableFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP006ImplementIDisposable.Id,
            Descriptors.IDISP009IsIDisposable.Id,
            "CS0535");

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (IsSupportedDiagnostic(diagnostic) &&
                    syntaxRoot.TryFindNodeOrAncestor(diagnostic, out TypeDeclarationSyntax typeDeclaration))
                {
                    if (diagnostic.Id == Descriptors.IDISP009IsIDisposable.Id)
                    {
                        context.RegisterCodeFix(
                            "Add IDisposable interface",
                            (editor, cancellationToken) => editor.AddInterfaceType(typeDeclaration, IDisposableFactory.SystemIDisposable),
                            "add interface",
                            diagnostic);
                        continue;
                    }

                    if (typeDeclaration is StructDeclarationSyntax structDeclaration)
                    {
                        context.RegisterCodeFix(
                            "Implement IDisposable.",
                            (editor, _) => editor.AddMethod(structDeclaration, MethodFactory.Dispose()),
                            "Struct",
                            diagnostic);
                    }
                    else if (typeDeclaration is ClassDeclarationSyntax classDeclaration)
                    {
                        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                                  .ConfigureAwait(false);
                        var type = semanticModel.GetDeclaredSymbolSafe(typeDeclaration, context.CancellationToken);
                        if (Disposable.IsAssignableFrom(type, semanticModel.Compilation) &&
                            type.TryFindFirstMethodRecursive("Dispose", x => x.IsVirtual, out var baseDispose))
                        {
                            context.RegisterCodeFix(
                                $"override {baseDispose}",
                                (editor, cancellationToken) =>
                                    OverrideDispose(
                                        editor,
                                        classDeclaration,
                                        baseDispose,
                                        cancellationToken),
                                "override",
                                diagnostic);
                            continue;
                        }

                        if (type.TryFindSingleMethodRecursive("Dispose", out var disposeMethod) &&
                            !disposeMethod.IsStatic &&
                            disposeMethod.ReturnsVoid &&
                            disposeMethod.Parameters.Length == 0)
                        {
                            continue;
                        }

                        if (type.TryFindFieldRecursive("disposed", out _) ||
                            type.TryFindFieldRecursive("_disposed", out _))
                        {
                            return;
                        }

                        if (type.IsSealed)
                        {
                            context.RegisterCodeFix(
                                "Implement IDisposable.",
                                (editor, cancellationToken) =>
                                    ImplementIDisposableSealedAsync(
                                        editor,
                                        classDeclaration,
                                        cancellationToken),
                                "Sealed",
                                diagnostic);
                            continue;
                        }

                        if (type.IsAbstract)
                        {
                            context.RegisterCodeFix(
                                "Implement IDisposable with virtual dispose method.",
                                (editor, cancellationToken) =>
                                    ImplementIDisposableVirtualAsync(
                                        editor,
                                        classDeclaration,
                                        cancellationToken),
                                nameof(ImplementIDisposableFix) + "Virtual",
                                diagnostic);
                            continue;
                        }

                        context.RegisterCodeFix(
                            "Implement IDisposable and make class sealed.",
                            (editor, cancellationToken) =>
                                ImplementIDisposableSealedAsync(
                                    editor,
                                    classDeclaration,
                                    cancellationToken),
                            nameof(ImplementIDisposableFix) + "Sealed",
                            diagnostic);

                        context.RegisterCodeFix(
                            "Implement IDisposable with virtual dispose method.",
                            (editor, cancellationToken) =>
                                ImplementIDisposableVirtualAsync(
                                    editor,
                                    classDeclaration,
                                    cancellationToken),
                            nameof(ImplementIDisposableFix) + "Virtual",
                            diagnostic);
                    }
                }
            }
        }

        private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic.Id == Descriptors.IDISP006ImplementIDisposable.Id ||
                diagnostic.Id == Descriptors.IDISP009IsIDisposable.Id)
            {
                return true;
            }

            if (diagnostic.Id == "CS0535")
            {
                return diagnostic.GetMessage(CultureInfo.InvariantCulture)
                                 .EndsWith("does not implement interface member 'IDisposable.Dispose()'", StringComparison.Ordinal);
            }

            return false;
        }

        private static void OverrideDispose(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, IMethodSymbol toOverride, CancellationToken cancellationToken)
        {
            var disposed = editor.AddField(
                classDeclaration,
                "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            _ = editor.AddMethod(classDeclaration, MethodFactory.OverrideDispose(disposed, toOverride))
                      .AddThrowIfDisposed(classDeclaration, disposed, cancellationToken);
        }

        private static void ImplementIDisposableVirtualAsync(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var disposed = editor.AddField(
                classDeclaration,
                "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            _ = editor.AddIDisposableInterface(classDeclaration)
                      .AddMethod(classDeclaration, MethodFactory.Dispose(editor.ThisDisposedTrue(cancellationToken), IDisposableFactory.GcSuppressFinalizeThis))
                      .AddMethod(classDeclaration, MethodFactory.ProtectedVirtualDispose(disposed))
                      .AddThrowIfDisposed(classDeclaration, disposed, cancellationToken);
        }

        private static void ImplementIDisposableSealedAsync(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var disposed = editor.AddField(
                classDeclaration,
                "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            _ = editor.AddIDisposableInterface(classDeclaration)
                      .AddMethod(classDeclaration, MethodFactory.Dispose(disposed))
                      .AddPrivateThrowIfDisposed(classDeclaration, disposed, cancellationToken);

            if (!classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                _ = editor.ReplaceNode(
                    classDeclaration,
                    x => MakeSealedRewriter.Default.Visit(x, x));
            }
        }

        private class MakeSealedRewriter : CSharpSyntaxRewriter
        {
            internal static readonly MakeSealedRewriter Default = new MakeSealedRewriter();

            private static readonly ThreadLocal<ClassDeclarationSyntax> CurrentClass = new ThreadLocal<ClassDeclarationSyntax>();

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                // We only want to make the top level class sealed.
                if (ReferenceEquals(CurrentClass.Value, node))
                {
                    var updated = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
                    return updated.WithModifiers(updated.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword)));
                }

                return node;
            }

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out var modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out var modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitEventDeclaration(node);
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out var modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out var modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                var parentModifiers = node.FirstAncestor<BasePropertyDeclarationSyntax>()?.Modifiers;
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    parentModifiers?.Any(SyntaxKind.OverrideKeyword) == false)
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                if (parentModifiers?.TrySingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier) == true)
                {
                    if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier))
                    {
                        node = node.WithModifiers(node.Modifiers.Remove(modifier));
                    }
                }

                return base.VisitAccessorDeclaration(node);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out var modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitMethodDeclaration(node);
            }

            internal SyntaxNode Visit(SyntaxNode node, ClassDeclarationSyntax classDeclaration)
            {
                CurrentClass.Value = classDeclaration;
                var updated = this.Visit(node);
                CurrentClass.Value = null;
                return updated;
            }
        }
    }
}
