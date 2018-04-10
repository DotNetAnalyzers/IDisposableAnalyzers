namespace IDisposableAnalyzers.Test.Documentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Tests
    {
        private static readonly IReadOnlyList<DescriptorInfo> Descriptors = typeof(AnalyzerCategory)
            .Assembly.GetTypes()
            .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
            .SelectMany(DescriptorInfo.Create)
            .OrderBy(x => x.Descriptor.Id)
            .ToArray();

        private static IReadOnlyList<DescriptorInfo> DescriptorsWithDocs => Descriptors.Where(d => d.DocExists).ToArray();

        private static string SolutionDirectory => CodeFactory.FindSolutionFile("IDisposableAnalyzers.sln").DirectoryName;

        private static string DocumentsDirectory => Path.Combine(SolutionDirectory, "documentation");

        [TestCaseSource(nameof(Descriptors))]
        public void MissingDocs(DescriptorInfo descriptorInfo)
        {
            if (!descriptorInfo.DocExists)
            {
                var descriptor = descriptorInfo.Descriptor;
                var id = descriptor.Id;
                DumpIfDebug(CreateStub(descriptorInfo));
                File.WriteAllText(descriptorInfo.DocFileName + ".generated", CreateStub(descriptorInfo));
                Assert.Fail($"Documentation is missing for {id}");
            }
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public void TitleId(DescriptorInfo descriptorInfo)
        {
            Assert.AreEqual($"# {descriptorInfo.Descriptor.Id}", File.ReadLines(descriptorInfo.DocFileName).First());
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public void Title(DescriptorInfo descriptorInfo)
        {
            Assert.AreEqual(File.ReadLines(descriptorInfo.DocFileName).Skip(1).First(), $"## {descriptorInfo.Descriptor.Title}");
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public void Description(DescriptorInfo descriptorInfo)
        {
            var expected = File.ReadLines(descriptorInfo.DocFileName)
                               .SkipWhile(l => !l.StartsWith("## Description"))
                               .Skip(1)
                               .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            var actual = descriptorInfo.Descriptor.Description.ToString();

            DumpIfDebug(expected);
            DumpIfDebug(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public void Table(DescriptorInfo descriptorInfo)
        {
            switch (descriptorInfo.Descriptor.Id)
            {
                case IDISP001DisposeCreated.DiagnosticId when descriptorInfo.Analyzer is IDISP001DisposeCreated:
                case IDISP003DisposeBeforeReassigning.DiagnosticId when descriptorInfo.Analyzer is AssignmentAnalyzer:
                case IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId when descriptorInfo.Analyzer is IDISP004DontIgnoreReturnValueOfTypeIDisposable:
                case IDISP008DontMixInjectedAndCreatedForMember.DiagnosticId when descriptorInfo.Analyzer is AssignmentAnalyzer:
                    return;
            }

            var expected = GetTable(CreateStub(descriptorInfo));
            DumpIfDebug(expected);
            var actual = GetTable(File.ReadAllText(descriptorInfo.DocFileName));
            CodeAssert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public void ConfigSeverity(DescriptorInfo descriptorInfo)
        {
            var expected = GetConfigSeverity(CreateStub(descriptorInfo));
            DumpIfDebug(expected);
            var actual = GetConfigSeverity(File.ReadAllText(descriptorInfo.DocFileName));
            CodeAssert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(Descriptors))]
        public void UniqueIds(DescriptorInfo descriptorInfo)
        {
            Assert.AreEqual(1, Descriptors.Select(x => x.Descriptor).Distinct().Count(d => d.Id == descriptorInfo.Descriptor.Id));
        }

        [Test]
        public void Index()
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!-- start generated table -->")
                   .AppendLine("<table>");
            foreach (var descriptor in DescriptorsWithDocs.Select(x => x.Descriptor).Distinct().OrderBy(x => x.Id))
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($@"  <td><a href=""{descriptor.HelpLinkUri}"">{descriptor.Id}</a></td>");
                builder.AppendLine($"  <td>{descriptor.Title}</td>");
                builder.AppendLine("</tr>");
            }

            builder.AppendLine("<table>")
                   .Append("<!-- end generated table -->");
            var expected = builder.ToString();
            DumpIfDebug(expected);
            var actual = GetTable(File.ReadAllText(Path.Combine(SolutionDirectory, "Readme.md")));
            CodeAssert.AreEqual(expected, actual);
        }

        private static string CreateStub(DescriptorInfo descriptorInfo)
        {
            var descriptor = descriptorInfo.Descriptor;
            return CreateStub(
                id: descriptor.Id,
                title: descriptor.Title.ToString(),
                severity: descriptor.DefaultSeverity,
                enabled: descriptor.IsEnabledByDefault,
                codeFileUrl: descriptorInfo.CodeFileUri,
                category: descriptor.Category,
                typeName: descriptorInfo.Analyzer.GetType().Name,
                description: descriptor.Description.ToString());
        }

        private static string CreateStub(
            string id,
            string title,
            DiagnosticSeverity severity,
            bool enabled,
            string codeFileUrl,
            string category,
            string typeName,
            string description)
        {
            return Properties.Resources.DiagnosticDocTemplate.Replace("{ID}", id)
                             .Replace("## ADD TITLE HERE", $"## {title}")
                             .Replace("{SEVERITY}", severity.ToString())
                             .Replace("{ENABLED}", enabled ? "true" : "false")
                             .Replace("{CATEGORY}", category)
                             .Replace("{URL}", codeFileUrl ?? "https://github.com/DotNetAnalyzers/PropertyChangedAnalyzers")
                             .Replace("{TYPENAME}", typeName)
                             .Replace("ADD DESCRIPTION HERE", description ?? "ADD DESCRIPTION HERE")
                             .Replace("{TITLE}", title)
                             .Replace("{TRIMMEDTYPENAME}", typeName.Substring(id.Length));
        }

        private static string GetTable(string doc)
        {
            return GetSection(doc, "<!-- start generated table -->", "<!-- end generated table -->");
        }

        private static string GetConfigSeverity(string doc)
        {
            return GetSection(doc, "<!-- start generated config severity -->", "<!-- end generated config severity -->");
        }

        private static string GetSection(string doc, string startToken, string endToken)
        {
            var start = doc.IndexOf(startToken, StringComparison.Ordinal);
            var end = doc.IndexOf(endToken, StringComparison.Ordinal) + endToken.Length;
            return doc.Substring(start, end - start);
        }

        [Conditional("DEBUG")]
        private static void DumpIfDebug(string text)
        {
            Console.Write(text);
            Console.WriteLine();
            Console.WriteLine();
        }

        public class DescriptorInfo
        {
            public DescriptorInfo(DiagnosticAnalyzer analyzer, DiagnosticDescriptor descriptor)
            {
                this.Analyzer = analyzer;
                this.Descriptor = descriptor;
                this.DocFileName = Path.Combine(DocumentsDirectory, descriptor.Id + ".md");
                this.CodeFileName = Directory.EnumerateFiles(
                                                 SolutionDirectory,
                                                 analyzer.GetType().Name + ".cs",
                                                 SearchOption.AllDirectories)
                                             .FirstOrDefault();
                this.CodeFileUri = this.CodeFileName != null
                    ? @"https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/" +
                      this.CodeFileName.Substring(SolutionDirectory.Length + 1).Replace("\\", "/")
                    : "missing";
            }

            public DiagnosticAnalyzer Analyzer { get; }

            public string DocFileName { get; }

            public string CodeFileName { get; }

            public string CodeFileUri { get; }

            public DiagnosticDescriptor Descriptor { get; }

            public bool DocExists => File.Exists(this.DocFileName);

            public static IEnumerable<DescriptorInfo> Create(DiagnosticAnalyzer analyzer) => analyzer.SupportedDiagnostics.Select(d => new DescriptorInfo(analyzer, d));

            public override string ToString() => this.Descriptor.Id;
        }
    }
}
