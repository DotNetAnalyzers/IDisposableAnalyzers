namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    internal class ParameterMethodTemplate
    {
        private readonly string code;
        private readonly string placeHolder;
        private readonly ConcurrentDictionary<string, MethodDeclarationSyntax> cache = new ConcurrentDictionary<string, MethodDeclarationSyntax>();

        public ParameterMethodTemplate(string code)
        {
            this.code = code;
            var matches = Regex.Matches(code, @"(?<parameter>\<\w+\>)")
                               .OfType<Match>()
                               .Select(x => x.Groups["parameter"].Value)
                               .Distinct()
                               .ToArray();
            if (matches.Length != 1)
            {
                throw new ArgumentException(nameof(code));
            }

            this.placeHolder = matches[0];
        }

        public MethodDeclarationSyntax MethodDeclarationSyntax(string parameter)
        {
            return this.cache.GetOrAdd(parameter, this.Parse);
        }

        private MethodDeclarationSyntax Parse(string parameter)
        {
            var updated = this.code.Replace(this.placeHolder, parameter);
            var syntaxTree = CSharpSyntaxTree.ParseText(updated);
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