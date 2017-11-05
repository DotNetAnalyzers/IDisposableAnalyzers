namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AliasWalker : PooledWalker<AliasWalker>
    {
        private readonly List<NameEqualsSyntax> aliases = new List<NameEqualsSyntax>();

        private AliasWalker()
        {
        }

        public IReadOnlyList<NameEqualsSyntax> Aliases => this.aliases;

        public static AliasWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new AliasWalker());

        public static bool Contains(SyntaxTree tree, string typeName)
        {
            if (tree == null ||
                typeName == null)
            {
                return false;
            }

            if (tree.TryGetRoot(out var root))
            {
                using (var walker = Borrow(root))
                {
                    return walker.Aliases.Any(x => x.Name.Identifier.ValueText == typeName);
                }
            }

            return false;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Alias != null)
            {
                this.aliases.Add(node.Alias);
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
        }

        protected override void Clear()
        {
            this.aliases.Clear();
        }
    }
}