namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    internal class MethodTemplate
    {
        private readonly string code;
        private MethodDeclarationSyntax methodDeclarationSyntax;

        public MethodTemplate(string code)
        {
            this.code = code;
        }

        public MethodDeclarationSyntax MethodDeclarationSyntax => this.methodDeclarationSyntax ?? (this.methodDeclarationSyntax = Parse(this.code));

        private static MethodDeclarationSyntax Parse(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var method = syntaxTree.GetRoot(CancellationToken.None)
                                   .DescendantNodes()
                                   .OfType<MethodDeclarationSyntax>()
                                   .Single();
            if (method == null)
            {
                throw new InvalidOperationException("Method is null");
            }

            return method.WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                         .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                         .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
