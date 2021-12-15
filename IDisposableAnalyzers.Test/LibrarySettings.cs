namespace IDisposableAnalyzers.Test
{
    using Gu.Roslyn.Asserts;

    internal static class LibrarySettings
    {
        internal static readonly Settings Reactive = new(
            Settings.Default.CompilationOptions.WithSuppressedDiagnostics("CS1701"),
            Settings.Default.ParseOptions,
            new MetadataReferencesCollection(
                MetadataReferences.Transitive(
                    typeof(System.Reactive.Disposables.DisposableMixins),
                    typeof(Gu.Wpf.Reactive.ConditionRelayCommand))));
    }
}
