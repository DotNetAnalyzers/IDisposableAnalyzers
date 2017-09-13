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
            defaultSeverity: DiagnosticSeverity.Hidden,
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

            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (MustBeHandled(objectCreation, context.SemanticModel, context.CancellationToken))
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

            var invocation = (InvocationExpressionSyntax)context.Node;
            var method = context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) as IMethodSymbol;
            if (method == null ||
                method.ReturnsVoid ||
                !Disposable.IsPotentiallyAssignableTo(method.ReturnType))
            {
                return;
            }

            if (MustBeHandled(invocation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool MustBeHandled(
            ExpressionSyntax node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
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
                          .IsEither(Result.No, Result.Unknown))
            {
                return false;
            }

            if (node.Parent is StatementSyntax)
            {
                return true;
            }

            if (node.Parent is ArgumentSyntax argument)
            {
                if (argument.Parent.Parent is InvocationExpressionSyntax invocation)
                {
                    using (var returnWalker = ReturnValueWalker.Create(invocation, Search.Recursive, semanticModel, cancellationToken))
                    {
                        foreach (var returnValue in returnWalker.Item)
                        {
                            if (MustBeHandled(returnValue, semanticModel, cancellationToken))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                if (argument.Parent.Parent is ObjectCreationExpressionSyntax)
                {
                    if (TryGetAssignedFieldOrProperty(argument, semanticModel, cancellationToken, out ISymbol member, out IMethodSymbol ctor) &&
                        member != null)
                    {
                        var initializer = argument.FirstAncestorOrSelf<ConstructorInitializerSyntax>();
                        if (initializer != null)
                        {
                            if (semanticModel.GetDeclaredSymbolSafe(initializer.Parent, cancellationToken) is IMethodSymbol chainedCtor &&
                                chainedCtor.ContainingType != member.ContainingType)
                            {
                                if (Disposable.TryGetDisposeMethod(chainedCtor.ContainingType, Search.TopLevel, out IMethodSymbol disposeMethod))
                                {
                                    return !Disposable.IsMemberDisposed(member, disposeMethod, semanticModel, cancellationToken);
                                }
                            }
                        }

                        if (Disposable.IsMemberDisposed(member, ctor.ContainingType, semanticModel, cancellationToken)
                                      .IsEither(Result.Yes, Result.Maybe))
                        {
                            return false;
                        }

                        return true;
                    }

                    return ctor?.ContainingType != KnownSymbol.StreamReader &&
                           ctor?.ContainingType != KnownSymbol.CompositeDisposable;
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

                if (!methodDeclaration.TryGetMatchingParameter(argument, out ParameterSyntax paremeter))
                {
                    continue;
                }

                var parameterSymbol = semanticModel.GetDeclaredSymbolSafe(paremeter, cancellationToken);
                if (methodDeclaration.Body.TryGetAssignment(parameterSymbol, semanticModel, cancellationToken, out AssignmentExpressionSyntax assignment))
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