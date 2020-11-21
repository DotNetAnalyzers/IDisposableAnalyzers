using Gu.Roslyn.Asserts;

[assembly: TransitiveMetadataReferences(typeof(IDisposableAnalyzers.Test.ValidWithAllAnalyzers))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute))]
[assembly: TransitiveMetadataReferences(typeof(System.Windows.Forms.Form))]
[assembly: TransitiveMetadataReferences(typeof(System.Windows.Controls.Control))]
[assembly: TransitiveMetadataReferences(typeof(System.Reactive.Linq.Observable))]
[assembly: TransitiveMetadataReferences(typeof(System.Reactive.Disposables.DisposableMixins))]
[assembly: TransitiveMetadataReferences(typeof(System.Data.Entity.DbContext))]
[assembly: MetadataReferences(
    typeof(System.Net.WebClient),
    typeof(System.Net.Mail.Attachment),
    typeof(System.Data.Common.DbConnection),
    typeof(System.Threading.Tasks.ValueTask),
    typeof(System.Xml.Serialization.XmlSerializer),
    typeof(Moq.Mock<>),
    typeof(Ninject.StandardKernel),
    typeof(Gu.Inject.Kernel),
    typeof(Gu.Reactive.SerialDisposable<>))]
