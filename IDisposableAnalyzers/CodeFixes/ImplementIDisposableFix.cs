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

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                      .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (IsSupportedDiagnostic(diagnostic) &&
                    syntaxRoot.TryFindNodeOrAncestor(diagnostic, out TypeDeclarationSyntax? typeDeclaration))
                {
                    if (diagnostic.Id == Descriptors.IDISP009IsIDisposable.Id)
                    {
                        context.RegisterCodeFix(
                            "Add IDisposable interface",
                            (editor, cancellationToken) => editor.AddInterfaceType(typeDeclaration, IDisposableFactory.SystemIDisposable),
                            "add interface",
                            diagnostic);
                    }
                    else if (typeDeclaration is StructDeclarationSyntax structDeclaration)
                    {
                        context.RegisterCodeFix(
                            "Implement IDisposable.",
                            (editor, _) => editor.AddMethod(structDeclaration, MethodFactory.Dispose()),
                            "Struct",
                            diagnostic);
                    }
                    else if (typeDeclaration is ClassDeclarationSyntax classDeclaration &&
                             semanticModel.TryGetNamedType(classDeclaration, context.CancellationToken, out var type))
                    {
                        if (Disposable.IsAssignableFrom(type, semanticModel.Compilation) &&
                            type.TryFindFirstMethodRecursive("Dispose", x => x.IsVirtual, out var baseDispose))
                        {
                            context.RegisterCodeFix(
                                $"override {baseDispose}",
                                (editor, cancellationToken) => OverrideDispose(editor, cancellationToken),
                                "override",
                                diagnostic);

                            void OverrideDispose(DocumentEditor editor, CancellationToken cancellationToken)
                            {
                                var disposed = editor.AddField(
                                    classDeclaration,
                                    "disposed",
                                    Accessibility.Private,
                                    DeclarationModifiers.None,
                                    SyntaxFactory.ParseTypeName("bool"));

                                _ = editor.AddMethod(classDeclaration, MethodFactory.OverrideDispose(disposed, baseDispose))
                                          .AddThrowIfDisposed(classDeclaration, disposed, cancellationToken);
                            }
                        }
                        else if (CanImplement())
                        {
                            switch (type)
                            {
                                case { IsSealed: true }:
                                    context.RegisterCodeFix(
                                        "Implement IDisposable.",
                                        (editor, cancellationToken) =>
                                            Sealed(editor, cancellationToken),
                                        nameof(Sealed),
                                        diagnostic);
                                    break;
                                case { IsAbstract: true }:
                                    context.RegisterCodeFix(
                                        "Implement IDisposable.",
                                        (editor, cancellationToken) => Vanilla(editor, cancellationToken),
                                        nameof(Vanilla),
                                        diagnostic);

                                    context.RegisterCodeFix(
                                        "LEGACY Implement IDisposable with protected virtual dispose method.",
                                        (editor, cancellationToken) => Legacy(editor, cancellationToken),
                                        nameof(Legacy),
                                        diagnostic);
                                    break;
                                default:
                                    context.RegisterCodeFix(
                                        "Implement IDisposable and make class sealed.",
                                        (editor, cancellationToken) => Sealed(editor, cancellationToken),
                                        nameof(Sealed),
                                        diagnostic);

                                    context.RegisterCodeFix(
                                        "Implement IDisposable.",
                                        (editor, cancellationToken) => Vanilla(editor, cancellationToken),
                                        nameof(Vanilla),
                                        diagnostic);

                                    context.RegisterCodeFix(
                                        "LEGACY Implement IDisposable with protected virtual dispose method.",
                                        (editor, cancellationToken) => Legacy(editor, cancellationToken),
                                        nameof(Legacy),
                                        diagnostic);
                                    break;
                            }

                            void Vanilla(DocumentEditor editor, CancellationToken cancellationToken)
                            {
                                var disposed = editor.AddField(
                                    classDeclaration,
                                    "disposed",
                                    Accessibility.Private,
                                    DeclarationModifiers.None,
                                    SyntaxFactory.ParseTypeName("bool"));

                                _ = editor.AddIDisposableInterface(classDeclaration)
                                          .AddMethod(classDeclaration, MethodFactory.VirtualDispose(disposed))
                                          .AddThrowIfDisposed(classDeclaration, disposed, cancellationToken);
                            }

                            void Sealed(DocumentEditor editor, CancellationToken cancellationToken)
                            {
                                var disposed = editor.AddField(
                                    classDeclaration,
                                    "disposed",
                                    Accessibility.Private,
                                    DeclarationModifiers.None,
                                    SyntaxFactory.ParseTypeName("bool"));

                                _ = editor.AddIDisposableInterface(classDeclaration)
                                          .AddMethod(classDeclaration, MethodFactory.Dispose(disposed))
                                          .AddPrivateThrowIfDisposed(classDeclaration, disposed, cancellationToken);

                                if (!classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
                                {
                                    _ = editor.ReplaceNode(
                                        classDeclaration,
                                        x => SealRewriter.Seal(x));
                                }
                            }

                            void Legacy(DocumentEditor editor, CancellationToken cancellationToken)
                            {
                                var disposed = editor.AddField(
                                    classDeclaration,
                                    "disposed",
                                    Accessibility.Private,
                                    DeclarationModifiers.None,
                                    SyntaxFactory.ParseTypeName("bool"));

                                _ = editor.AddIDisposableInterface(classDeclaration)
                                          .AddMethod(classDeclaration, MethodFactory.Dispose(editor.ThisDisposedTrue(), IDisposableFactory.GcSuppressFinalizeThis))
                                          .AddMethod(classDeclaration, MethodFactory.ProtectedVirtualDispose(disposed))
                                          .AddThrowIfDisposed(classDeclaration, disposed, cancellationToken);
                            }
                        }

                        bool CanImplement()
                        {
                            return !type.TryFindFirstMethodRecursive("Dispose", out var disposeMethod) ||
                                   disposeMethod.Parameters.Length != 0;
                        }
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
    }
}
