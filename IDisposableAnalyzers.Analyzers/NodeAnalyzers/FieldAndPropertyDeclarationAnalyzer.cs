namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class FieldAndPropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP002DisposeMember.Descriptor,
            IDISP006ImplementIDisposable.Descriptor,
            IDISP008DontMixInjectedAndCreatedForMember.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.PropertyDeclaration);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.ContainingSymbol is IFieldSymbol field &&
                !field.IsStatic &&
                !field.IsConst &&
                Disposable.IsPotentiallyAssignableTo(field.Type))
            {
                HandleFieldOrProperty(context, field);
            }
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var property = (IPropertySymbol)context.ContainingSymbol;
            if (property.IsStatic ||
                property.IsIndexer)
            {
                return;
            }

            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            if (propertyDeclaration.ExpressionBody != null)
            {
                return;
            }

            if (propertyDeclaration.TryGetSetter(out var setter) &&
                setter.Body != null)
            {
                // Handle the backing field
                return;
            }

            if (Disposable.IsPotentiallyAssignableTo(property.Type))
            {
                HandleFieldOrProperty(context, property);
            }
        }

        private static void HandleFieldOrProperty(SyntaxNodeAnalysisContext context, ISymbol fieldOrProperty)
        {
            using (var assignedValues = AssignedValueWalker.Borrow(fieldOrProperty, context.SemanticModel, context.CancellationToken))
            {
                using (var recursive = RecursiveValues.Create(assignedValues, context.SemanticModel, context.CancellationToken))
                {
                    if (Disposable.IsAnyCreation(recursive, context.SemanticModel, context.CancellationToken)
                                  .IsEither(Result.Yes, Result.AssumeYes))
                    {
                        if (IsMutableFromOutside(fieldOrProperty))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                        }
                        else if (Disposable.IsAnyCachedOrInjected(recursive, context.SemanticModel, context.CancellationToken)
                                           .IsEither(Result.Yes, Result.AssumeYes))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                        }
                        else if (Disposable.IsMemberDisposed(fieldOrProperty, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken)
                                           .IsEither(Result.No, Result.AssumeNo, Result.Unknown) &&
                                 !TestFixture.IsAssignedAndDisposedInSetupAndTearDown(
                                     fieldOrProperty,
                                     context.Node.FirstAncestor<TypeDeclarationSyntax>(),
                                     context.SemanticModel,
                                     context.CancellationToken))
                        {
                            if (Disposable.IsAssignableTo(fieldOrProperty.ContainingType) &&
                                Disposable.TryGetDisposeMethod(fieldOrProperty.ContainingType, ReturnValueSearch.TopLevel, out IMethodSymbol _))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(IDISP002DisposeMember.Descriptor, context.Node.GetLocation()));
                            }
                            else
                            {
                                if (TestFixture.IsAssignedInSetUp(
                                    fieldOrProperty,
                                    context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(),
                                    context.SemanticModel,
                                    context.CancellationToken,
                                    out _))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(IDISP002DisposeMember.Descriptor, context.Node.GetLocation()));
                                }

                                context.ReportDiagnostic(Diagnostic.Create(IDISP006ImplementIDisposable.Descriptor, context.Node.GetLocation()));
                            }
                        }
                    }
                }
            }
        }

        private static bool IsMutableFromOutside(ISymbol symbol)
        {
            if (symbol is IFieldSymbol field)
            {
                if (field.IsReadOnly)
                {
                    return false;
                }

                switch (field.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        return false;
                    case Accessibility.Protected:
                        return !field.ContainingType.IsSealed;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Public:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (symbol is IPropertySymbol property)
            {
                if (property.SetMethod == null)
                {
                    return false;
                }

                switch (property.SetMethod.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        return false;
                    case Accessibility.Protected:
                        return !property.ContainingType.IsSealed;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Public:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            throw new InvalidOperationException("Should not get here.");
        }
    }
}
