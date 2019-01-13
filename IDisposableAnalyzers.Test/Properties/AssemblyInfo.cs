using Gu.Roslyn.Asserts;

[assembly: MetadataReference(typeof(object), new[] { "global", "mscorlib" })]
[assembly: MetadataReference(typeof(System.Diagnostics.Debug), new[] { "global", "System" })]
[assembly:TransitiveMetadataReferences(typeof(Gu.Roslyn.CodeFixExtensions.CodeStyle))]
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
    typeof(Ninject.StandardKernel),
    typeof(Gu.Roslyn.AnalyzerExtensions.SyntaxTokenExt),
    typeof(Gu.Roslyn.CodeFixExtensions.Parse),
    typeof(Stubs.Extensions),
    typeof(IDisposableAnnotations.GivesOwnershipAttribute),
    typeof(NUnit.Framework.Assert))]
