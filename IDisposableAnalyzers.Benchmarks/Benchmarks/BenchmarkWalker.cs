namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class BenchmarkWalker
    {
        private readonly BenchmarkAnalysisContext context = new BenchmarkAnalysisContext();
        private readonly Project project;
        private readonly Walker walker;

        public BenchmarkWalker(Project project, DiagnosticAnalyzer analyzer)
        {
            this.project = project;
            analyzer.Initialize(this.context);
            this.walker = new Walker(this.context.Actions);
        }

        public void Run()
        {
            foreach (var document in this.project.Documents)
            {
                this.walker.Document = document;
                if (document.TryGetSyntaxRoot(out var root))
                {
                    this.walker.Visit(root);
                }
            }
        }

        private class Walker : CSharpSyntaxWalker
        {
            private static readonly Action<Diagnostic> ReportDiagnostic = _ => { };
            private static readonly Func<Diagnostic, bool> IsSupportedDiagnostic = _ => true;

            private readonly IReadOnlyDictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>> actions;
            private Document document;
            private SemanticModel semanticModel;
            private ISymbol symbol;

            public Walker(IReadOnlyDictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>> actions)
                : base(SyntaxWalkerDepth.Token)
            {
                this.actions = actions;
            }

            internal Document Document
            {
                get => this.document;

                set
                {
                    this.document = value;
                    this.semanticModel = this.document.GetSemanticModelAsync().Result;
                }
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                if (node is BaseFieldDeclarationSyntax field)
                {
                    foreach (var declarator in field.Declaration.Variables)
                    {
                        this.symbol = this.semanticModel.GetDeclaredSymbol(declarator, CancellationToken.None);
                        if (this.actions.TryGetValue(node.Kind(), out var action))
                        {
                            action(
                                new SyntaxNodeAnalysisContext(
                                    node,
                                    this.symbol,
                                    this.semanticModel,
                                    null,
                                    ReportDiagnostic,
                                    IsSupportedDiagnostic,
                                    CancellationToken.None));
                        }

                        base.DefaultVisit(node);
                    }
                }
                else
                {
                    this.symbol = this.GetDeclaredSymbolOrDefault(node, CancellationToken.None) ?? this.symbol;
                    if (this.actions.TryGetValue(node.Kind(), out var action))
                    {
                        action(
                            new SyntaxNodeAnalysisContext(
                                node,
                                this.symbol,
                                this.semanticModel,
                                null,
                                ReportDiagnostic,
                                IsSupportedDiagnostic,
                                CancellationToken.None));
                    }

                    base.DefaultVisit(node);
                }
            }

            private ISymbol GetDeclaredSymbolOrDefault(SyntaxNode node, CancellationToken cancellationToken)
            {
                // http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Compilation/CSharpSemanticModel.cs,4633
                switch (node)
                {
                    case AccessorDeclarationSyntax accessor:
                        return this.semanticModel.GetDeclaredSymbol(accessor, cancellationToken);
                    case BaseTypeDeclarationSyntax type:
                        return this.semanticModel.GetDeclaredSymbol(type, cancellationToken);
                    case QueryClauseSyntax clause:
                        return this.semanticModel.GetDeclaredSymbol(clause, cancellationToken);
                    case MemberDeclarationSyntax member:
                        return this.semanticModel.GetDeclaredSymbol(member, cancellationToken);
                }

                switch (node.Kind())
                {
                    case SyntaxKind.LocalFunctionStatement:
                        return this.semanticModel.GetDeclaredSymbol((LocalFunctionStatementSyntax)node, cancellationToken);
                    case SyntaxKind.LabeledStatement:
                        return this.semanticModel.GetDeclaredSymbol((LabeledStatementSyntax)node, cancellationToken);
                    case SyntaxKind.CaseSwitchLabel:
                    case SyntaxKind.DefaultSwitchLabel:
                        return this.semanticModel.GetDeclaredSymbol((SwitchLabelSyntax)node, cancellationToken);
                    case SyntaxKind.AnonymousObjectCreationExpression:
                        return this.semanticModel.GetDeclaredSymbol((AnonymousObjectCreationExpressionSyntax)node, cancellationToken);
                    case SyntaxKind.AnonymousObjectMemberDeclarator:
                        return this.semanticModel.GetDeclaredSymbol((AnonymousObjectMemberDeclaratorSyntax)node, cancellationToken);
                    case SyntaxKind.TupleExpression:
                        return this.semanticModel.GetDeclaredSymbol((TupleExpressionSyntax)node, cancellationToken);
                    case SyntaxKind.Argument:
                        return this.semanticModel.GetDeclaredSymbol((ArgumentSyntax)node, cancellationToken);
                    case SyntaxKind.VariableDeclarator:
                        return this.semanticModel.GetDeclaredSymbol((VariableDeclaratorSyntax)node, cancellationToken);
                    case SyntaxKind.SingleVariableDesignation:
                        return this.semanticModel.GetDeclaredSymbol((SingleVariableDesignationSyntax)node, cancellationToken);
                    case SyntaxKind.TupleElement:
                        return this.semanticModel.GetDeclaredSymbol((TupleElementSyntax)node, cancellationToken);
                    case SyntaxKind.NamespaceDeclaration:
                        return this.semanticModel.GetDeclaredSymbol((NamespaceDeclarationSyntax)node, cancellationToken);
                    case SyntaxKind.Parameter:
                        return this.semanticModel.GetDeclaredSymbol((ParameterSyntax)node, cancellationToken);
                    case SyntaxKind.TypeParameter:
                        return this.semanticModel.GetDeclaredSymbol((TypeParameterSyntax)node, cancellationToken);
                    case SyntaxKind.UsingDirective:
                        var usingDirective = (UsingDirectiveSyntax)node;
                        if (usingDirective.Alias == null)
                        {
                            break;
                        }

                        return this.semanticModel.GetDeclaredSymbol(usingDirective, cancellationToken);
                    case SyntaxKind.ForEachStatement:
                        return this.semanticModel.GetDeclaredSymbol((ForEachStatementSyntax)node, cancellationToken);
                    case SyntaxKind.CatchDeclaration:
                        return this.semanticModel.GetDeclaredSymbol((CatchDeclarationSyntax)node, cancellationToken);
                    case SyntaxKind.JoinIntoClause:
                        return this.semanticModel.GetDeclaredSymbol((JoinIntoClauseSyntax)node, cancellationToken);
                    case SyntaxKind.QueryContinuation:
                        return this.semanticModel.GetDeclaredSymbol((QueryContinuationSyntax)node, cancellationToken);
                }

                return null;
            }
        }

        private class BenchmarkAnalysisContext : AnalysisContext
        {
            private readonly Dictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>> actions = new Dictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>>(SyntaxKindComparer.Default);

            public IReadOnlyDictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>> Actions => this.actions;

            public override void EnableConcurrentExecution()
            {
            }

            public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode)
            {
            }

            public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
            {
                foreach (var kind in syntaxKinds)
                {
                    this.actions.Add((SyntaxKind)(object)kind, action);
                }
            }

            public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
            {
                throw new NotImplementedException();
            }

            public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
            {
                throw new NotImplementedException();
            }

            public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
            {
                throw new NotImplementedException();
            }

            public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
            {
                throw new NotImplementedException();
            }

            public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
            {
                throw new NotImplementedException();
            }

            public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
            {
                throw new NotImplementedException();
            }

            public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
            {
                throw new NotImplementedException();
            }

            private class SyntaxKindComparer : IEqualityComparer<SyntaxKind>
            {
                public static readonly SyntaxKindComparer Default = new SyntaxKindComparer();

                public bool Equals(SyntaxKind x, SyntaxKind y)
                {
                    return x == y;
                }

                public int GetHashCode(SyntaxKind obj)
                {
                    return (int)obj;
                }
            }
        }
    }
}