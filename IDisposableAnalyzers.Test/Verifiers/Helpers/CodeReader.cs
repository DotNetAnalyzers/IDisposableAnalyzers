namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    using NUnit.Framework;

    internal struct CodeReader
    {
        private readonly string code;

        public CodeReader(string code)
        {
            this.code = code;
        }

        public static bool operator ==(CodeReader left, CodeReader right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CodeReader left, CodeReader right)
        {
            return !Equals(left, right);
        }

        public static string[] CreateFileNamesFromSources(string[] sources, string extension)
        {
            var filenames = new string[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var fileName = FileNameFromSource(source, extension);
                var part = 0;
                while (true)
                {
                    if (filenames.Contains(fileName))
                    {
                        fileName = $"{fileName.Remove(extension.Length + 1)}_Part{part}.{extension}";
                        part++;
                        continue;
                    }

                    filenames[i] = fileName;
                    break;
                }
            }

            return filenames;
        }

        public static FileLinePositionSpan GetErrorPosition(string[] sources)
        {
            var fileNames = CreateFileNamesFromSources(sources, "cs");
            var line = 0;
            var column = -1;
            var fileName = string.Empty;

            const char errorPositionIndicator = '↓';
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var colCount = 1;
                var lineCount = 1;
                foreach (var c in source)
                {
                    if (c == '\n')
                    {
                        lineCount++;
                        colCount = 1;
                        continue;
                    }

                    if (c == '↓')
                    {
                        if (column >= 0)
                        {
                            Assert.AreEqual(-1, column, "Expected to find only one error indicated by ↓");
                        }

                        sources[i] = sources[i].Replace(new string(errorPositionIndicator, 1), string.Empty);
                        column = colCount;
                        line = lineCount;
                        fileName = fileNames[i];
                    }

                    colCount++;
                }
            }

            Assert.AreNotEqual(-1, column, "Expected to find one error indicated by ↓");
            var pos = new LinePosition(line, column);
            return new FileLinePositionSpan(fileName, pos, pos);
        }

        public static FileLinePositionSpan GetErrorPosition(ref string testCode)
        {
            var sources = new[] { testCode };
            var result = GetErrorPosition(sources);
            testCode = sources[0];
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((CodeReader)obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override string ToString() => this.code;

        private static string FileNameFromSource(string source, string extension)
        {
            string fileName;
            if (source == string.Empty)
            {
                return $"Test.{extension}";
            }

            var matches = Regex.Matches(source, @"(class|struct|enum|interface) (?<name>\w+)(<(?<typeArg>\w+)>)?", RegexOptions.ExplicitCapture);
            if (matches.Count == 0)
            {
                fileName = "AssemblyInfo";
            }
            else
            {
                Assert.LessOrEqual(1, matches.Count, "Use class per file, it catches more bugs");
                fileName = matches[0].Groups["name"].Value;
                if (matches[0].Groups["typeArg"].Success)
                {
                    fileName += $"{{{matches[0].Groups["typeArg"].Value}}}";
                }
            }

            return $"{fileName}.{extension}";
        }

        private bool Equals(CodeReader other)
        {
            var pos = 0;
            var otherPos = 0;
            var line = 1;
            while (pos < this.code.Length && otherPos < other.code.Length)
            {
                if (this.code[pos] == '\r')
                {
                    pos++;
                    continue;
                }

                if (other.code[otherPos] == '\r')
                {
                    otherPos++;
                    continue;
                }

                if (this.code[pos] != other.code[otherPos])
                {
                    Console.WriteLine($"Mismatch on line {line}");
                    var expected = this.code.Split('\n')[line - 1].Trim('\r');
                    var actual = other.code.Split('\n')[line - 1].Trim('\r');
                    var diffPos = Math.Min(expected.Length, actual.Length);
                    for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
                    {
                        if (expected[i] != actual[i])
                        {
                            diffPos = i;
                            break;
                        }
                    }

                    Console.WriteLine($"Expected: {expected}");
                    Console.WriteLine($"Actual:   {actual}");
                    Console.WriteLine($"         {new string(' ', diffPos)}^");
                    return false;
                }

                if (this.code[pos] == '\n')
                {
                    line++;
                }

                pos++;
                otherPos++;
            }

            while (pos < this.code.Length && this.code[pos] == '\r')
            {
                pos++;
            }

            while (otherPos < other.code.Length && other.code[otherPos] == '\r')
            {
                otherPos++;
            }

            return pos == this.code.Length && otherPos == other.code.Length;
        }
    }
}