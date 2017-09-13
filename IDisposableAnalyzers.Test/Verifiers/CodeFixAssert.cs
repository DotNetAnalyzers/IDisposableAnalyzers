namespace IDisposableAnalyzers.Test
{
    using System.Reflection;

    using Microsoft.CodeAnalysis.CodeFixes;

    using NUnit.Framework;

    public static class CodeFixAssert
    {
        public static void NameMatchesExportedName(CodeFixProvider codeFixProvider)
        {
            if (codeFixProvider == null)
            {
                return;
            }

            var exportAttribute = codeFixProvider.GetType().GetCustomAttribute<ExportCodeFixProviderAttribute>();
            Assert.AreEqual(codeFixProvider.GetType().Name, exportAttribute.Name);
        }
    }
}