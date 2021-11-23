using System;
using Gu.Roslyn.Asserts;

[assembly: CLSCompliant(false)]

[assembly: TransitiveMetadataReferences(typeof(IDisposableAnalyzers.Test.ValidWithAllAnalyzers))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext))]
[assembly: TransitiveMetadataReferences(typeof(System.Windows.Forms.Form))]
[assembly: TransitiveMetadataReferences(typeof(System.Windows.Controls.Control))]
[assembly: TransitiveMetadataReferences(typeof(System.Reactive.Linq.Observable))]
[assembly: TransitiveMetadataReferences(typeof(System.Reactive.Disposables.DisposableMixins))]
[assembly: TransitiveMetadataReferences(typeof(System.Data.Entity.DbContext))]
[assembly: TransitiveMetadataReferences(typeof(Gu.Wpf.Reactive.ConditionRelayCommand))]
[assembly: MetadataReferences(
    typeof(System.Net.WebClient),
    typeof(System.Net.Mail.Attachment),
    typeof(System.Data.Common.DbConnection),
    typeof(System.Threading.Tasks.ValueTask),
    typeof(System.Xml.Serialization.XmlSerializer),
    typeof(Moq.Mock<>),
    typeof(Microsoft.Extensions.DependencyInjection.ServiceCollection),
    typeof(Ninject.StandardKernel),
    typeof(Gu.Inject.Kernel),
    typeof(Gu.Inject.RebindExtensions))]
