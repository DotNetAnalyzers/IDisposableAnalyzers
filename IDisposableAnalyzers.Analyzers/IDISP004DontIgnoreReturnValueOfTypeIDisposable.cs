namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP004DontIgnoreReturnValueOfTypeIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP004";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't ignore return value of type IDisposable.",
            messageFormat: "Don't ignore return value of type IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't ignore return value of type IDisposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(HandleInvocation, SyntaxKind.InvocationExpression);
        }

        private static void HandleCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                MustBeHandled(objectCreation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is InvocationExpressionSyntax invocation &&
                context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) is IMethodSymbol method &&
                !method.ReturnsVoid &&
                Disposable.IsPotentiallyAssignableTo(method.ReturnType) &&
                MustBeHandled(invocation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool MustBeHandled(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.Parent is AnonymousFunctionExpressionSyntax ||
                node.Parent is UsingStatementSyntax ||
                node.Parent is EqualsValueClauseSyntax ||
                node.Parent is ReturnStatementSyntax ||
                node.Parent is ArrowExpressionClauseSyntax)
            {
                return false;
            }

            if (Disposable.IsCreation(node, semanticModel, cancellationToken)
                          .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
            {
                return false;
            }

            if (node.Parent is StatementSyntax ||
                node.Parent is MemberAccessExpressionSyntax)
            {
                return true;
            }

            if (node.Parent is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList)
            {
                if (argumentList.Parent is InvocationExpressionSyntax invocation)
                {
                    using (var returnWalker = ReturnValueWalker.Borrow(invocation, Search.Recursive, semanticModel, cancellationToken))
                    {
                        foreach (var returnValue in returnWalker)
                        {
                            if (!MustBeHandled(returnValue, semanticModel, cancellationToken))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }

                if (argumentList.Parent is ObjectCreationExpressionSyntax)
                {
                    if (TryGetAssignedFieldOrProperty(argument, semanticModel, cancellationToken, out var member, out var ctor) &&
                        member != null)
                    {
                        var initializer = argument.FirstAncestorOrSelf<ConstructorInitializerSyntax>();
                        if (initializer != null)
                        {
                            if (semanticModel.GetDeclaredSymbolSafe(initializer.Parent, cancellationToken) is IMethodSymbol chainedCtor &&
                                chainedCtor.ContainingType != member.ContainingType)
                            {
                                if (Disposable.TryGetDisposeMethod(chainedCtor.ContainingType, Search.TopLevel, out var disposeMethod))
                                {
                                    return !Disposable.IsMemberDisposed(member, disposeMethod, semanticModel, cancellationToken);
                                }
                            }
                        }

                        if (Disposable.IsMemberDisposed(member, ctor.ContainingType, semanticModel, cancellationToken)
                                      .IsEither(Result.Yes, Result.AssumeYes))
                        {
                            return false;
                        }

                        return true;
                    }

                    if (ctor == null)
                    {
                        return false;
                    }

                    if (ctor.ContainingType.DeclaringSyntaxReferences.Length == 0)
                    {
                        return !Disposable.IsAssignableTo(ctor.ContainingType);
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool TryGetConstructor(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol ctor)
        {
            var objectCreation = argument.FirstAncestor<ObjectCreationExpressionSyntax>();
            if (objectCreation != null)
            {
                ctor = semanticModel.GetSymbolSafe(objectCreation, cancellationToken) as IMethodSymbol;
                return ctor != null;
            }

            var initializer = argument.FirstAncestor<ConstructorInitializerSyntax>();
            if (initializer != null)
            {
                ctor = semanticModel.GetSymbolSafe(initializer, cancellationToken);
                return ctor != null;
            }

            ctor = null;
            return false;
        }

        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol member, out IMethodSymbol ctor)
        {
            if (TryGetConstructor(argument, semanticModel, cancellationToken, out ctor))
            {
                return TryGetAssignedFieldOrProperty(argument, ctor, semanticModel, cancellationToken, out member);
            }

            member = null;
            return false;
        }

        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol member)
        {
            member = null;
            if (method == null)
            {
                return false;
            }

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var methodDeclaration = reference.GetSyntax(cancellationToken) as BaseMethodDeclarationSyntax;
                if (methodDeclaration == null)
                {
                    continue;
                }

                if (!methodDeclaration.TryGetMatchingParameter(argument, out var paremeter))
                {
                    continue;
                }

                var parameterSymbol = semanticModel.GetDeclaredSymbolSafe(paremeter, cancellationToken);
                if (methodDeclaration.Body.TryGetAssignment(parameterSymbol, semanticModel, cancellationToken, out var assignment))
                {
                    member = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
                    if (member is IFieldSymbol ||
                        member is IPropertySymbol)
                    {
                        return true;
                    }
                }

                var ctor = reference.GetSyntax(cancellationToken) as ConstructorDeclarationSyntax;
                if (ctor?.Initializer != null)
                {
                    foreach (var arg in ctor.Initializer.ArgumentList.Arguments)
                    {
                        var argSymbol = semanticModel.GetSymbolSafe(arg.Expression, cancellationToken);
                        if (parameterSymbol.Equals(argSymbol))
                        {
                            var chained = semanticModel.GetSymbolSafe(ctor.Initializer, cancellationToken);
                            return TryGetAssignedFieldOrProperty(arg, chained, semanticModel, cancellationToken, out member);
                        }
                    }
                }
            }

            return false;
        }
    }
}
