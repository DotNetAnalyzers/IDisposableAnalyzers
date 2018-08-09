namespace IDisposableAnalyzers.Test.Documentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Tests
    {
        private static readonly IReadOnlyList<DiagnosticAnalyzer> Analyzers = typeof(AnalyzerCategory)
                                                                              .Assembly
                                                                              .GetTypes()
                                                                              .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
                                                                              .OrderBy(x => x.Name)
                                                                              .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                                                              .ToArray();

        private static readonly IReadOnlyList<DescriptorInfo> DescriptorInfos = Analyzers
            .SelectMany(DescriptorInfo.Create)
            .ToArray();

        private static IReadOnlyList<DescriptorInfo> DescriptorsWithDocs => DescriptorInfos.Where(d => d.DocExists).ToArray();

        private static DirectoryInfo SolutionDirectory => SolutionFile.Find("IDisposableAnalyzers.sln").Directory;

        private static DirectoryInfo DocumentsDirectory => SolutionDirectory.EnumerateDirectories("documentation", SearchOption.TopDirectoryOnly).Single();

        [TestCaseSource(nameof(DescriptorInfos))]
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
            var actual = descriptorInfo.Descriptor.Description.ToString().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).First();

            DumpIfDebug(expected);
            DumpIfDebug(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public void Table(DescriptorInfo descriptorInfo)
        {
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

        [TestCaseSource(nameof(DescriptorInfos))]
        public void UniqueIds(DescriptorInfo descriptorInfo)
        {
            Assert.AreEqual(1, DescriptorInfos.Select(x => x.Descriptor).Distinct().Count(d => d.Id == descriptorInfo.Descriptor.Id));
        }

        [Test]
        public void Index()
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!-- start generated table -->")
                   .AppendLine("<table>");
            foreach (var descriptor in DescriptorsWithDocs.Select(x => x.Descriptor).Distinct().OrderBy(x => x.Id))
            {
                builder.AppendLine("  <tr>");
                builder.AppendLine($@"    <td><a href=""{descriptor.HelpLinkUri}"">{descriptor.Id}</a></td>");
                builder.AppendLine($"    <td>{descriptor.Title}</td>");
                builder.AppendLine("  </tr>");
            }

            builder.AppendLine("<table>")
                   .Append("<!-- end generated table -->");
            var expected = builder.ToString();
            DumpIfDebug(expected);
            var actual = GetTable(File.ReadAllText(Path.Combine(SolutionDirectory.FullName, "Readme.md")));
            CodeAssert.AreEqual(expected, actual);
        }

        private static string CreateStub(DescriptorInfo descriptorInfo)
        {
            var descriptor = descriptorInfo.Descriptor;
            var stub = Properties.Resources.DiagnosticDocTemplate
                             .AssertReplace("{ID}", descriptor.Id)
                             .AssertReplace("## ADD TITLE HERE", $"## {descriptor.Title.ToString()}")
                             .AssertReplace("{SEVERITY}", descriptor.DefaultSeverity.ToString())
                             .AssertReplace("{ENABLED}", descriptor.IsEnabledByDefault ? "true" : "false")
                             .AssertReplace("{CATEGORY}", descriptor.Category)
                             .AssertReplace("ADD DESCRIPTION HERE", descriptor.Description.ToString())
                             .AssertReplace("{TITLE}", descriptor.Title.ToString());
            if (Analyzers.Count(x => x.SupportedDiagnostics.Any(d => d.Id == descriptor.Id)) == 1)
            {
                return stub.AssertReplace("{TYPENAME}", descriptorInfo.Analyzer.GetType().Name)
                           .AssertReplace("{URL}", descriptorInfo.CodeFileUri);
            }

            var builder = StringBuilderPool.Borrow();
            var first = true;
            foreach (var analyzer in Analyzers.Where(x => x.SupportedDiagnostics.Any(d => d.Id == descriptor.Id)))
            {
                _ = builder.AppendLine("  <tr>")
                           .AppendLine($"    <td>{(first ? "Code" : string.Empty)}</td>")
                           .AppendLine($"     <td><a href=\"{DescriptorInfo.GetCodeFileUri(analyzer)}\">{analyzer.GetType().Name}</a></td>")
                           .AppendLine("  </tr>");

                first = false;
            }

            var text = builder.Return();
            return stub.Replace("  <tr>\r\n    <td>Code</td>\r\n    <td><a href=\"{URL}\">{TYPENAME}</a></td>\r\n  </tr>\r\n", text)
                       .Replace("  <tr>\n    <td>Code</td>\n    <td><a href=\"{URL}\">{TYPENAME}</a></td>\n  </tr>\n", text);
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
            private DescriptorInfo(DiagnosticAnalyzer analyzer, DiagnosticDescriptor descriptor)
            {
                this.Analyzer = analyzer;
                this.Descriptor = descriptor;
                this.DocFileName = Path.Combine(DocumentsDirectory.FullName, descriptor.Id + ".md");
                this.CodeFileName = Directory.EnumerateFiles(
                                                 SolutionDirectory.FullName,
                                                 analyzer.GetType().Name + ".cs",
                                                 SearchOption.AllDirectories)
                                             .FirstOrDefault();
                this.CodeFileUri = GetCodeFileUri(analyzer);
            }

            public DiagnosticAnalyzer Analyzer { get; }

            public bool DocExists => File.Exists(this.DocFileName);

            public DiagnosticDescriptor Descriptor { get; }

            public string DocFileName { get; }

            public string CodeFileName { get; }

            public string CodeFileUri { get; }

            public static string GetCodeFileUri(DiagnosticAnalyzer analyzer)
            {
                string fileName = Directory.EnumerateFiles(SolutionDirectory.FullName, analyzer.GetType().Name + ".cs", SearchOption.AllDirectories)
                                           .FirstOrDefault();
                return fileName != null
                    ? "https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master" +
                      fileName.Substring(SolutionDirectory.FullName.Length).Replace("\\", "/")
                    : "missing";
            }

            public static IEnumerable<DescriptorInfo> Create(DiagnosticAnalyzer analyzer)
            {
                foreach (var descriptor in analyzer.SupportedDiagnostics)
                {
                    yield return new DescriptorInfo(analyzer, descriptor);
                }
            }

            public override string ToString() => this.Descriptor.Id;
        }
    }
}
