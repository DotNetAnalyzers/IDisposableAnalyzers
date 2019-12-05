namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid<T>
    {
        [TestCase("this.components.Add(stream)")]
        [TestCase("components.Add(stream)")]
        public static void LocalAddedToFormComponents(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        Winform()
        {
            var stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(stream);
        }
    }
}".AssertReplace("this.components.Add(stream)", expression);
            RoslynAssert.NoAnalyzerDiagnostics(Analyzer, code);
        }

        [TestCase("this.components.Add(this.stream)")]
        [TestCase("components.Add(stream)")]
        public static void FieldAddedToFormComponents(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        private readonly Stream stream;

        Winform()
        {
            this.stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(this.stream);
        }
    }
}".AssertReplace("this.components.Add(this.stream)", expression);
            RoslynAssert.NoAnalyzerDiagnostics(Analyzer, code);
        }

        [Test]
        public static void IgnoreNewFormShow()
        {
            var winForm = @"
namespace N
{
    using System.Windows.Forms;

    public class Winform : Form
    {
    }
}";

            var code = @"
namespace N
{
    public class C
    {
        void M()
        {
            var form = new Winform();
            form.Show();
        }
    }
}";
            RoslynAssert.NoAnalyzerDiagnostics(Analyzer, winForm, code);
        }
    }
}
