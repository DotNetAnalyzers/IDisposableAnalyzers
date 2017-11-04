namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;

    internal class DocumentOnlyFixAllProvider : FixAllProvider
    {
        public static readonly DocumentOnlyFixAllProvider Default = new DocumentOnlyFixAllProvider();

        private static readonly ImmutableArray<FixAllScope> SupportedFixAllScopes = ImmutableArray.Create(FixAllScope.Document);

        private DocumentOnlyFixAllProvider()
        {
        }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        {
            return SupportedFixAllScopes;
        }

        public override Task<CodeAction> GetFixAsync(FixAllContext context)
        {
            return WellKnownFixAllProviders.BatchFixer.GetFixAsync(context);
        }
    }
}