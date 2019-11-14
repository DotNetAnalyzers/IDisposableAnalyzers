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
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableFix))]
    [Shared]
    internal class ImplementIDisposableFix : DocumentEditorCodeFixProvider
    {
        private static readonly UsingDirectiveSyntax UsingSystem = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"));

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
                            (editor, _) => editor.AddMethod(structDeclaration, IDisposableFactory.EmptyDisposeMethodDeclaration),
                            "Struct",
                            diagnostic);
                    }
                    else if (typeDeclaration is ClassDeclarationSyntax classDeclaration)
                    {
                        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                         .ConfigureAwait(false);
                        var type = semanticModel.GetDeclaredSymbolSafe(typeDeclaration, context.CancellationToken);
                        if (Disposable.IsAssignableFrom(type, semanticModel.Compilation) &&
                            DisposeMethod.TryFindBaseVirtual(type, out var baseDispose))
                        {
                            context.RegisterCodeFix(
                                "override Dispose(bool)",
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

        private static void OverrideDispose(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, IMethodSymbol baseDispose, CancellationToken cancellationToken)
        {
            var type = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var disposed = editor.AddField(
                classDeclaration,
                "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            var code = StringBuilderPool.Borrow()
                                        .AppendLine($"{baseDispose.DeclaredAccessibility.ToCodeString()} override void Dispose(bool disposing)")
                                        .AppendLine("{")
                                        .AppendLine("    if (this.disposed)")
                                        .AppendLine("    {")
                                        .AppendLine("        return;")
                                        .AppendLine("    }")
                                        .AppendLine()
                                        .AppendLine("     this.disposed = true;")
                                        .AppendLine("     if (disposing)")
                                        .AppendLine("     {")
                                        .AppendLine("     }")
                                        .AppendLine()
                                        .AppendLine("     base.Dispose(disposing);")
                                        .AppendLine("}")
                                        .Return();
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            _ = editor.AddMethod(
                classDeclaration,
                ParseMethod(code, usesUnderscoreNames, disposed));

            if (!type.GetMembers().TryFirst(x => x.Name == "ThrowIfDisposed", out _))
            {
                if (type.BaseType.TryFindSingleMethodRecursive("ThrowIfDisposed", out var baseThrow) &&
                    baseThrow.Parameters.Length == 0)
                {
                    if (baseThrow.IsVirtual)
                    {
                        code = StringBuilderPool.Borrow()
                                                .AppendLine($"{baseThrow.DeclaredAccessibility.ToCodeString()} override void ThrowIfDisposed()")
                                                .AppendLine("{")
                                                .AppendLine("    if (this.disposed)")
                                                .AppendLine("    {")
                                                .AppendLine("        throw new System.ObjectDisposedException(this.GetType().FullName);")
                                                .AppendLine("    }")
                                                .AppendLine()
                                                .AppendLine("     base.ThrowIfDisposed();")
                                                .AppendLine("}")
                                                .Return();
                        _ = editor.AddMethod(
                            classDeclaration,
                            ParseMethod(code, usesUnderscoreNames, disposed));
                    }
                }
                else
                {
                    _ = editor.AddMethod(
                        classDeclaration,
                        ParseMethod(
                            @"protected virtual void ThrowIfDisposed()
                            {
                                if (this.disposed)
                                {
                                    throw new System.ObjectDisposedException(this.GetType().FullName);
                                }
                            }",
                            usesUnderscoreNames,
                            disposed));
                }
            }
        }

        private static void ImplementIDisposableVirtualAsync(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var type = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var disposed = editor.AddField(
                classDeclaration,
                "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            _ = editor.AddMethod(
                classDeclaration,
                ParseMethod(
                    @"public void Dispose()
                          {
                              this.Dispose(true);
                              GC.SuppressFinalize(this);
                          }",
                    usesUnderscoreNames))
                      .AddMethod(
                classDeclaration,
                ParseMethod(
                    @"protected virtual void Dispose(bool disposing)
                      {
                          if (this.disposed)
                          {
                              return;
                          }
                     
                          this.disposed = true;
                          if (disposing)
                          {
                          }
                      }",
                    usesUnderscoreNames,
                    disposed));

            if (!type.TryFindSingleMethodRecursive("ThrowIfDisposed", out _))
            {
                _ = editor.AddMethod(
                    classDeclaration,
                    ParseMethod(
                        @"protected virtual void ThrowIfDisposed()
                          {
                              if (this.disposed)
                              {
                                  throw new System.ObjectDisposedException(this.GetType().FullName);
                              }
                          }",
                        usesUnderscoreNames,
                        disposed));
            }

            if (classDeclaration.BaseList?.Types.TrySingle(x => (x.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("IDisposable") == true, out _) != true)
            {
                editor.AddInterfaceType(classDeclaration, IDisposableFactory.SystemIDisposable);
            }

            _ = editor.AddUsing(UsingSystem);
        }

        private static void ImplementIDisposableSealedAsync(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var type = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var disposed = editor.AddField(
                classDeclaration,
                "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            _ = editor.AddMethod(
                classDeclaration,
                IDisposableFactory.DisposeMethodDeclaration(disposed));

            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            if (!type.TryFindSingleMethodRecursive("ThrowIfDisposed", out _))
            {
                _ = editor.AddMethod(
                    classDeclaration,
                    ParseMethod(
                        @"private void ThrowIfDisposed()
                          {
                              if (this.disposed)
                              {
                                  throw new System.ObjectDisposedException(this.GetType().FullName);
                              }
                          }",
                        usesUnderscoreNames,
                        disposed));
            }

            if (classDeclaration.BaseList?.Types.TryFirst(x => (x.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("IDisposable") == true, out _) != true)
            {
                editor.AddInterfaceType(classDeclaration, IDisposableFactory.SystemIDisposable);
            }

            if (!type.IsSealed)
            {
                _ = editor.ReplaceNode(
                    classDeclaration,
                    x => MakeSealedRewriter.Default.Visit(x, x));
            }
        }

        private static MethodDeclarationSyntax ParseMethod(string code, bool usesUnderscoreNames, ExpressionSyntax disposedFieldAccess = null)
        {
            if (disposedFieldAccess is { })
            {
                code = code.Replace("this.disposed", disposedFieldAccess.ToString());
            }

            if (usesUnderscoreNames)
            {
                code = code.Replace("this.", string.Empty);
            }

            return Parse.MethodDeclaration(code)
                        .WithSimplifiedNames()
                        .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                        .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                        .WithAdditionalAnnotations(Formatter.Annotation);
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
