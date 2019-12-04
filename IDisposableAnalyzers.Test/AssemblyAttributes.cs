using Gu.Roslyn.Asserts;

[assembly: TransitiveMetadataReferences(typeof(IDisposableAnalyzers.Test.ValidWithAllAnalyzers))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute))]
[assembly: MetadataReferences(
    typeof(System.Linq.Enumerable),
    typeof(System.Net.WebClient),
    typeof(System.Data.Common.DbConnection),
    typeof(System.Reactive.Disposables.SerialDisposable),
    typeof(System.Threading.Tasks.ValueTask),
    typeof(System.Reactive.Disposables.ICancelable),
    typeof(System.Reactive.Linq.Observable),
    typeof(System.Xml.Serialization.XmlSerializer),
    typeof(System.Windows.Media.Brush),
    typeof(System.Windows.Controls.Control),
    typeof(System.Windows.Media.Matrix),
    typeof(System.Xaml.XamlLanguage),
    typeof(Moq.Mock<>),
    typeof(Ninject.KernelConfiguration),
    typeof(System.Windows.Forms.Form),
    typeof(Gu.Roslyn.AnalyzerExtensions.SyntaxTokenExt),
    typeof(Gu.Roslyn.CodeFixExtensions.Parse),
    typeof(NUnit.Framework.Assert))]
