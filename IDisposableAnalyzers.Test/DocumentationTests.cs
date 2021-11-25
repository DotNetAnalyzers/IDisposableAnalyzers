#pragma warning disable CA1056 // Uri properties should not be strings
namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class DocumentationTests
    {
        private static readonly IReadOnlyList<DiagnosticAnalyzer> Analyzers =
            typeof(AnalyzerCategory)
                .Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
                .OrderBy(x => x.Name)
                .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                .ToArray();

        private static readonly IReadOnlyList<DescriptorInfo> DescriptorInfos = Analyzers
                                                                                .SelectMany(DescriptorInfo.Create)
                                                                                .ToArray();

        private static IReadOnlyList<DescriptorInfo> DescriptorsWithDocs => DescriptorInfos.Where(d => d.DocumentationFile.Exists)
                                                                                           .ToArray();

        private static DirectoryInfo SolutionDirectory => SolutionFile.Find("IDisposableAnalyzers.sln")
                                                                      .Directory;

        private static DirectoryInfo DocumentsDirectory => SolutionDirectory.EnumerateDirectories("documentation", SearchOption.TopDirectoryOnly)
                                                                            .Single();

        [TestCaseSource(nameof(DescriptorInfos))]
        public static void MissingDocs(DescriptorInfo descriptorInfo)
        {
            if (!descriptorInfo.DocumentationFile.Exists)
            {
                var descriptor = descriptorInfo.Descriptor;
                var id = descriptor.Id;
                DumpIfDebug(descriptorInfo.Stub);
                File.WriteAllText(descriptorInfo.DocumentationFile.Name + ".generated", descriptorInfo.Stub);
                Assert.Fail($"Documentation is missing for {id}");
            }
        }

        [TestCaseSource(nameof(DescriptorInfos))]
        public static void UniqueIds(DescriptorInfo descriptorInfo)
        {
            Assert.AreEqual(1, DescriptorInfos.Select(x => x.Descriptor)
                                              .Distinct()
                                              .Count(d => d.Id == descriptorInfo.Descriptor.Id));
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public static void TitleId(DescriptorInfo descriptorInfo)
        {
            Assert.AreEqual($"# {descriptorInfo.Descriptor.Id}", descriptorInfo.DocumentationFile.AllLines[0]);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public static void Title(DescriptorInfo descriptorInfo)
        {
            var expected = $"## {descriptorInfo.Descriptor.Title}";
            var actual = descriptorInfo.DocumentationFile.AllLines
                                       .Skip(1)
                                       .First()
                                       .Replace("`", string.Empty, StringComparison.InvariantCulture);
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public static void Description(DescriptorInfo descriptorInfo)
        {
            var expected = descriptorInfo.Descriptor
                                         .Description
                                         .ToString(CultureInfo.InvariantCulture)
                                         .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .First();
            var actual = descriptorInfo.DocumentationFile.AllLines
                                       .SkipWhile(l => !l.StartsWith("## Description", StringComparison.OrdinalIgnoreCase))
                                       .Skip(1)
                                       .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))
                                       ?.Replace("`", string.Empty, StringComparison.InvariantCulture);

            DumpIfDebug(expected);
            DumpIfDebug(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public static void Table(DescriptorInfo descriptorInfo)
        {
            const string HeaderRow = "| Topic    | Value";
            var expected = GetTable(descriptorInfo.Stub, HeaderRow);
            DumpIfDebug(expected);
            var actual = GetTable(descriptorInfo.DocumentationFile.AllText, HeaderRow);
            CodeAssert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(DescriptorsWithDocs))]
        public static void ConfigSeverity(DescriptorInfo descriptorInfo)
        {
            var expected = GetConfigSeverity(descriptorInfo.Stub);
            DumpIfDebug(expected);
            var actual = GetConfigSeverity(descriptorInfo.DocumentationFile.AllText);
            CodeAssert.AreEqual(expected, actual);

            static string GetConfigSeverity(string doc)
            {
                return GetSection(doc, "<!-- start generated config severity -->", "<!-- end generated config severity -->");
            }

            static string GetSection(string doc, string startToken, string endToken)
            {
                var start = doc.IndexOf(startToken, StringComparison.Ordinal);
                var end = doc.IndexOf(endToken, StringComparison.Ordinal) + endToken.Length;
                return doc.Substring(start, end - start);
            }
        }

        [Test]
        public static void Index()
        {
            var builder = new StringBuilder();
            const string HeaderRow = "| Id       | Title";
            builder.AppendLine(HeaderRow)
                   .AppendLine("| :--      | :--");
            foreach (var descriptor in DescriptorsWithDocs.Select(x => x.Descriptor)
                                                          .Distinct()
                                                          .OrderBy(x => x.Id))
            {
                builder.Append($"| [{descriptor.Id}]({descriptor.HelpLinkUri})")
                       .AppendLine($"| {descriptor.Title}");
            }

            var expected = builder.ToString();
            DumpIfDebug(expected);
            var actual = GetTable(File.ReadAllText(Path.Combine(SolutionDirectory.FullName, "Readme.md")), HeaderRow);
            CodeAssert.AreEqual(expected, actual);
        }

        private static string GetTable(string doc, string headerRow)
        {
            var startIndex = doc.IndexOf(headerRow, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                return string.Empty;
            }

            return doc.Substring(startIndex, TableLength());

            int TableLength()
            {
                var length = 0;
                while (startIndex + length < doc.Length)
                {
                    if (doc[startIndex + length] == '\n' &&
                        doc.TryElementAt(startIndex + length + 1, out var c) &&
                        c != '|')
                    {
                        length++;
                        return length;
                    }
                    else
                    {
                        length++;
                    }
                }

                return length;
            }
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
                this.DocumentationFile = new MarkdownFile(Path.Combine(DocumentsDirectory.FullName, descriptor.Id + ".md"));
                this.AnalyzerFile = CodeFile.Find(analyzer.GetType());
                this.Stub = CreateStub(descriptor);
            }

            public DiagnosticAnalyzer Analyzer { get; }

            public DiagnosticDescriptor Descriptor { get; }

            public MarkdownFile DocumentationFile { get; }

            public CodeFile AnalyzerFile { get; }

            public string Stub { get; }

            public static IEnumerable<DescriptorInfo> Create(DiagnosticAnalyzer analyzer)
            {
                foreach (var descriptor in analyzer.SupportedDiagnostics)
                {
                    yield return new DescriptorInfo(analyzer, descriptor);
                }
            }

            public override string ToString() => this.Descriptor.Id;

            private static string CreateStub(DiagnosticDescriptor descriptor)
            {
                var stub = $@"# {descriptor.Id}
## {descriptor.Title.ToString(CultureInfo.InvariantCulture)}

| Topic    | Value
| :--      | :--
| Id       | {descriptor.Id}
| Severity | {descriptor.DefaultSeverity}
| Enabled  | {(descriptor.IsEnabledByDefault ? "True" : "False")}
| Category | {descriptor.Category}
| Code     | [<TYPENAME>](<URL>)


## Description

{descriptor.Description.ToString(CultureInfo.InvariantCulture)}

## Motivation

ADD MOTIVATION HERE

## How to fix violations

ADD HOW TO FIX VIOLATIONS HERE

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable {descriptor.Id} // {descriptor.Title.ToString(CultureInfo.InvariantCulture)}
Code violating the rule here
#pragma warning restore {descriptor.Id} // {descriptor.Title.ToString(CultureInfo.InvariantCulture)}
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable {descriptor.Id} // {descriptor.Title.ToString(CultureInfo.InvariantCulture)}
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage(""{descriptor.Category}"", 
    ""{descriptor.Id}:{descriptor.Title.ToString(CultureInfo.InvariantCulture)}"", 
    Justification = ""Reason..."")]
```
<!-- end generated config severity -->";

                var builder = StringBuilderPool.Borrow();
                foreach (var analyzer in Analyzers.Where(x => x.SupportedDiagnostics.Any(d => d.Id == descriptor.Id)))
                {
                    _ = builder.Append($"|{(builder.Length == 0 ? " Code     " : "          ")}| ")
                               .AppendLine($"[{analyzer.GetType().Name}]({CodeFile.Find(analyzer.GetType()).Uri})");
                }

                var text = builder.Return();
                return stub.Replace("| Code     | [<TYPENAME>](<URL>)\r\n", text, StringComparison.InvariantCulture)
                           .Replace("| Code     | [<TYPENAME>](<URL>)\n", text, StringComparison.InvariantCulture);
            }
        }

        public class MarkdownFile
        {
            public MarkdownFile(string name)
            {
                this.Name = name;
                if (File.Exists(name))
                {
                    this.AllText = File.ReadAllText(name);
                    this.AllLines = File.ReadAllLines(name);
                }
            }

            public string Name { get; }

            public bool Exists => File.Exists(this.Name);

            public string AllText { get; }

            public IReadOnlyList<string> AllLines { get; }
        }

        public class CodeFile
        {
            private static readonly ConcurrentDictionary<Type, CodeFile> Cache = new ConcurrentDictionary<Type, CodeFile>();

            public CodeFile(string name)
            {
                this.Name = name;
            }

            public string Name { get; }

            public string Uri => "https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master" + this.Name.Substring(SolutionDirectory.FullName.Length)
                                                                                                             .Replace("\\", "/", StringComparison.InvariantCulture);

            public static CodeFile Find(Type type)
            {
                return Cache.GetOrAdd(type, x => FindCore(x.Name + ".cs"));
            }

            private static CodeFile FindCore(string name)
            {
                var fileName = Cache.Values.Select(x => Path.GetDirectoryName(x.Name))
                                    .Distinct()
                                    .SelectMany(d => Directory.EnumerateFiles(d, name, SearchOption.TopDirectoryOnly))
                                    .FirstOrDefault() ??
                               Directory.EnumerateFiles(SolutionDirectory.FullName, name, SearchOption.AllDirectories)
                                        .First();
                return new CodeFile(fileName);
            }
        }
    }
}
