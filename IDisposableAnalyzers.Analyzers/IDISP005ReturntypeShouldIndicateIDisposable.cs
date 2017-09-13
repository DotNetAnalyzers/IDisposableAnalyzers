namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP005ReturntypeShouldIndicateIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP005";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Return type should indicate that the value should be disposed.",
            messageFormat: "Return type should indicate that the value should be disposed.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Return type should indicate that the value should be disposed.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleReturnValue, SyntaxKind.ReturnStatement);
            context.RegisterSyntaxNodeAction(HandleArrow, SyntaxKind.ArrowExpressionClause);
            context.RegisterSyntaxNodeAction(HandleLamdba, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(HandleLamdba, SyntaxKind.SimpleLambdaExpression);
        }

        private static void HandleReturnValue(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var symbol = context.ContainingSymbol;
            if (IsIgnored(symbol))
            {
                return;
            }

            if (IsDisposableReturnTypeOrIgnored(ReturnType(context)))
            {
                return;
            }

            var returnStatement = (ReturnStatementSyntax)context.Node;
            if (returnStatement.Expression == null)
            {
                return;
            }

            HandleReturnValue(context, returnStatement.Expression);
        }

        private static void HandleArrow(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var symbol = context.ContainingSymbol;
            if (IsIgnored(symbol))
            {
                return;
            }

            if (IsDisposableReturnTypeOrIgnored(ReturnType(context)))
            {
                return;
            }

            var arrowClause = (ArrowExpressionClauseSyntax)context.Node;
            if (arrowClause.Expression == null)
            {
                return;
            }

            HandleReturnValue(context, arrowClause.Expression);
        }

        private static void HandleLamdba(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var symbol = context.ContainingSymbol;
            if (IsIgnored(symbol))
            {
                return;
            }

            if (IsDisposableReturnTypeOrIgnored(ReturnType(context)))
            {
                return;
            }

            var lambda = context.Node as LambdaExpressionSyntax;
            var returnValue = lambda?.Body as ExpressionSyntax;
            if (returnValue == null)
            {
                return;
            }

            HandleReturnValue(context, returnValue);
        }

        private static void HandleReturnValue(SyntaxNodeAnalysisContext context, ExpressionSyntax returnValue)
        {
            if (Disposable.IsCreation(returnValue, context.SemanticModel, context.CancellationToken)
                          .IsEither(Result.Yes, Result.Maybe))
            {
                var symbol = context.SemanticModel.GetSymbolSafe(returnValue, context.CancellationToken);
                if (symbol == null)
                {
                    return;
                }

                foreach (var reference in symbol.DeclaringSyntaxReferences)
                {
                    var node = reference.GetSyntax(context.CancellationToken);
                    if (node?.Parent?.Parent is UsingStatementSyntax)
                    {
                        return;
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, returnValue.GetLocation()));
            }
        }

        private static bool IsDisposableReturnTypeOrIgnored(ITypeSymbol type)
        {
            if (type == null ||
                type == KnownSymbol.Void)
            {
                return true;
            }

            if (Disposable.IsAssignableTo(type))
            {
                return true;
            }

            if (type == KnownSymbol.IEnumerator)
            {
                return true;
            }

            if (type == KnownSymbol.Task)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true && Disposable.IsAssignableTo(namedType.TypeArguments[0]);
            }

            if (type == KnownSymbol.Func)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true && Disposable.IsAssignableTo(namedType.TypeArguments[namedType.TypeArguments.Length - 1]);
            }

            return false;
        }

        private static bool IsIgnored(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                return method == KnownSymbol.IEnumerable.GetEnumerator;
            }

            return false;
        }

        private static ITypeSymbol ReturnType(SyntaxNodeAnalysisContext context)
        {
            var anonymousFunction = context.Node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();
            if (anonymousFunction != null)
            {
                var method = context.SemanticModel.GetSymbolSafe(anonymousFunction, context.CancellationToken) as IMethodSymbol;
                return method?.ReturnType;
            }

            return (context.ContainingSymbol as IMethodSymbol)?.ReturnType ??
                   (context.ContainingSymbol as IFieldSymbol)?.Type ??
                   (context.ContainingSymbol as IPropertySymbol)?.Type;
        }
    }
}