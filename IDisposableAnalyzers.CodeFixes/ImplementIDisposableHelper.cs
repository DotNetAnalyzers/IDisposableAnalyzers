// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ImplementIDisposableHelper
    {
        private static readonly UsingDirectiveSyntax UsingSystem = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System"));

        internal static CompilationUnitSyntax WithUsingSystem(this CompilationUnitSyntax syntaxRoot)
        {
            if (!(syntaxRoot.Members.FirstOrDefault() is NamespaceDeclarationSyntax @namespace))
            {
                if (syntaxRoot.Usings.HasUsingSystem())
                {
                    return syntaxRoot;
                }

                return syntaxRoot.Usings.Any()
                           ? syntaxRoot.InsertNodesBefore(syntaxRoot.Usings.First(), new[] { UsingSystem })
                           : syntaxRoot.AddUsings(UsingSystem);
            }

            if (@namespace.Usings.HasUsingSystem() || syntaxRoot.Usings.HasUsingSystem())
            {
                return syntaxRoot;
            }

            if (@namespace.Usings.Any())
            {
                return syntaxRoot.ReplaceNode(@namespace, @namespace.InsertNodesBefore(@namespace.Usings.First(), new[] { UsingSystem }));
            }

            if (syntaxRoot.Usings.Any())
            {
                return syntaxRoot.InsertNodesBefore(syntaxRoot.Usings.First(), new[] { UsingSystem });
            }

            return syntaxRoot.ReplaceNode(@namespace, @namespace.AddUsings(UsingSystem));
        }

        private static bool HasUsingSystem(this SyntaxList<UsingDirectiveSyntax> usings)
        {
            foreach (var @using in usings)
            {
                if (@using.Name.IsEquivalentTo(UsingSystem.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}