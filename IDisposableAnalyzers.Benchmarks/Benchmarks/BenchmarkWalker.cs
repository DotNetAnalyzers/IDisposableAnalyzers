namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class BenchmarkWalker
    {
        private readonly BenchmarkAnalysisContext context = new BenchmarkAnalysisContext();
        private readonly Project project;
        private readonly DiagnosticAnalyzer analyzer;
        private readonly Walker walker;

        public BenchmarkWalker(Project project, DiagnosticAnalyzer analyzer)
        {
            this.project = project;
            this.analyzer = analyzer;
            analyzer.Initialize(this.context);
            this.walker = new Walker(this.context);
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
            private readonly BenchmarkAnalysisContext context;
            private Document document;
            private SemanticModel semanticModel;

            public Walker(BenchmarkAnalysisContext context)
                : base(SyntaxWalkerDepth.Token)
            {
                this.context = context;
            }

            internal Document Document
            {
                get { return this.document; }
                set
                {
                    this.document = value;
                    this.semanticModel = this.document?.GetSemanticModelAsync().Result;
                }
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                if (this.context.Actions.TryGetValue(node.Kind(), out var action))
                {
                    action(new SyntaxNodeAnalysisContext(
                               node,
                               this.semanticModel.GetDeclaredSymbol(node),
                               this.semanticModel,
                               null,
                               _ => { },
                               _ => true,
                               CancellationToken.None));
                }

                base.DefaultVisit(node);
            }
        }


        public class BenchmarkAnalysisContext : AnalysisContext
        {
            private readonly Dictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>> actions = new Dictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>>();

            public IReadOnlyDictionary<SyntaxKind, Action<SyntaxNodeAnalysisContext>> Actions => this.actions;

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
        }
    }
}