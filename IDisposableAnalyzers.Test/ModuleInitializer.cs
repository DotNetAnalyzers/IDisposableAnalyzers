namespace IDisposableAnalyzers.Test
{
    using System.Runtime.CompilerServices;
    using Gu.Roslyn.Asserts;

    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Settings.Default = Settings.Default.WithMetadataReferences(
                MetadataReferences.Transitive(
                    typeof(ModuleInitializer),
                    typeof(System.Data.Entity.DbContext),
                    typeof(System.Reactive.Disposables.DisposableMixins),
                    typeof(System.Windows.Controls.Control),
                    typeof(System.Windows.Forms.Control),
                    typeof(Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute),
                    typeof(Microsoft.Extensions.DependencyInjection.ServiceCollection),
                    typeof(Moq.Mock<>),
                    typeof(Ninject.StandardKernel),
                    typeof(Gu.Wpf.Reactive.ConditionRelayCommand),
                    typeof(Gu.Inject.RebindExtensions)));
        }
    }
}
