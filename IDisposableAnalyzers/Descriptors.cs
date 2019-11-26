namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class Descriptors
    {
        internal static readonly DiagnosticDescriptor IDISP001DisposeCreated = Descriptors.Create(
            id: "IDISP001",
            title: "Dispose created.",
            messageFormat: "Dispose created.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "When you create an instance of a type that implements IDisposable you are responsible for disposing it.");

        internal static readonly DiagnosticDescriptor IDISP002DisposeMember = Descriptors.Create(
            id: "IDISP002",
            title: "Dispose member.",
            messageFormat: "Dispose member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Dispose the member as it is assigned with a created IDisposable.");

        internal static readonly DiagnosticDescriptor IDISP003DisposeBeforeReassigning = Descriptors.Create(
            id: "IDISP003",
            title: "Dispose previous before re-assigning.",
            messageFormat: "Dispose previous before re-assigning.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Dispose previous before re-assigning.");

        internal static readonly DiagnosticDescriptor IDISP004DoNotIgnoreCreated = Descriptors.Create(
            id: "IDISP004",
            title: "Don't ignore created IDisposable.",
            messageFormat: "Don't ignore created IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't ignore created IDisposable.");

        internal static readonly DiagnosticDescriptor IDISP005ReturnTypeShouldBeIDisposable = Descriptors.Create(
            id: "IDISP005",
            title: "Return type should indicate that the value should be disposed.",
            messageFormat: "Return type should indicate that the value should be disposed.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Return type should indicate that the value should be disposed.");

        internal static readonly DiagnosticDescriptor IDISP006ImplementIDisposable = Descriptors.Create(
            id: "IDISP006",
            title: "Implement IDisposable.",
            messageFormat: "Implement IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The member is assigned with a created IDisposables within the type. Implement IDisposable and dispose it.");

        internal static readonly DiagnosticDescriptor IDISP007DoNotDisposeInjected = Descriptors.Create(
            id: "IDISP007",
            title: "Don't dispose injected.",
            messageFormat: "Don't dispose injected.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't dispose disposables you do not own.");

        internal static readonly DiagnosticDescriptor IDISP008DoNotMixInjectedAndCreatedForMember = Descriptors.Create(
            id: "IDISP008",
            title: "Don't assign member with injected and created disposables.",
            messageFormat: "Don't assign member with injected and created disposables.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't assign member with injected and created disposables. It creates a confusing ownership situation.");

        internal static readonly DiagnosticDescriptor IDISP009IsIDisposable = Descriptors.Create(
            id: "IDISP009",
            title: "Add IDisposable interface.",
            messageFormat: "Add IDisposable interface.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The type has a Dispose method but does not implement IDisposable.");

        internal static readonly DiagnosticDescriptor IDISP010CallBaseDispose = Descriptors.Create(
            id: "IDISP010",
            title: "Call base.Dispose(disposing)",
            messageFormat: "Call base.Dispose({0})",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call base.Dispose(disposing)");

        internal static readonly DiagnosticDescriptor IDISP011DontReturnDisposed = Descriptors.Create(
            id: "IDISP011",
            title: "Don't return disposed instance.",
            messageFormat: "Don't return disposed instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't return disposed instance.");

        internal static readonly DiagnosticDescriptor IDISP012PropertyShouldNotReturnCreated = Descriptors.Create(
            id: "IDISP012",
            title: "Property should not return created disposable.",
            messageFormat: "Property should not return created disposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Property should not return created disposable.");

        internal static readonly DiagnosticDescriptor IDISP013AwaitInUsing = Descriptors.Create(
            id: "IDISP013",
            title: "Await in using.",
            messageFormat: "Await in using.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Await in using.");

        internal static readonly DiagnosticDescriptor IDISP014UseSingleInstanceOfHttpClient = Descriptors.Create(
            id: "IDISP014",
            title: "Use a single instance of HttpClient.",
            messageFormat: "Use a single instance of HttpClient.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use a single instance of HttpClient.");

        internal static readonly DiagnosticDescriptor IDISP015DoNotReturnCachedAndCreated = Descriptors.Create(
            id: "IDISP015",
            title: "Member should not return created and cached instance.",
            messageFormat: "Member should not return created and cached instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Member should not return created and cached instance.");

        internal static readonly DiagnosticDescriptor IDISP016DoNotUseDisposedInstance = Descriptors.Create(
            id: "IDISP016",
            title: "Don't use disposed instance.",
            messageFormat: "Don't use disposed instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use disposed instance.");

        internal static readonly DiagnosticDescriptor IDISP017PreferUsing = Descriptors.Create(
            id: "IDISP017",
            title: "Prefer using.",
            messageFormat: "Prefer using.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Prefer using.");

        internal static readonly DiagnosticDescriptor IDISP018CallSuppressFinalizeSealed = Descriptors.Create(
            id: "IDISP018",
            title: "Call SuppressFinalize.",
            messageFormat: "Call SuppressFinalize(this).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize(this) as the type has a finalizer.");

        internal static readonly DiagnosticDescriptor IDISP019CallSuppressFinalizeVirtual = Descriptors.Create(
            id: "IDISP019",
            title: "Call SuppressFinalize.",
            messageFormat: "Call SuppressFinalize(this).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize as there is a virtual dispose method.");

        internal static readonly DiagnosticDescriptor IDISP020SuppressFinalizeThis = Descriptors.Create(
            id: "IDISP020",
            title: "Call SuppressFinalize with this.",
            messageFormat: "Call SuppressFinalize(this).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize with this as argument.");

        internal static readonly DiagnosticDescriptor IDISP021DisposeTrue = Descriptors.Create(
            id: "IDISP021",
            title: "Call this.Dispose(true).",
            messageFormat: "Call this.Dispose(true).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call this.Dispose(true).");

        internal static readonly DiagnosticDescriptor IDISP022DisposeFalse = Descriptors.Create(
            id: "IDISP022",
            title: "Call this.Dispose(false).",
            messageFormat: "Call this.Dispose(false).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call this.Dispose(false).");

        internal static readonly DiagnosticDescriptor IDISP023ReferenceTypeInFinalizerContext = Descriptors.Create(
            id: "IDISP023",
            title: "Don't use reference types in finalizer context.",
            messageFormat: "Don't use reference types in finalizer context.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use reference types in finalizer context.");

        internal static readonly DiagnosticDescriptor IDISP024DoNotCallSuppressFinalizeIfSealedAndNoFinalizer = Descriptors.Create(
            id: "IDISP024",
            title: "Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer.",
            messageFormat: "Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer.");

        /// <summary>
        /// Create a DiagnosticDescriptor, which provides description about a <see cref="Diagnostic" />.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="title">A short title describing the diagnostic. For example, for CA1001: "Types that own disposable fields should be disposable".</param>
        /// <param name="messageFormat">A format message string, which can be passed as the first argument to <see cref="string.Format(string,object[])" /> when creating the diagnostic message with this descriptor.
        /// For example, for CA1001: "Implement IDisposable on '{0}' because it creates members of the following IDisposable types: '{1}'.</param>
        /// <param name="category">The category of the diagnostic (like Design, Naming etc.). For example, for CA1001: "Microsoft.Design".</param>
        /// <param name="defaultSeverity">Default severity of the diagnostic.</param>
        /// <param name="isEnabledByDefault">True if the diagnostic is enabled by default.</param>
        /// <param name="description">An optional longer description of the diagnostic.</param>
        /// <param name="customTags">Optional custom tags for the diagnostic. See <see cref="WellKnownDiagnosticTags" /> for some well known tags.</param>
        private static DiagnosticDescriptor Create(
            string id,
            string title,
            string messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            string description,
            params string[] customTags)
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: category,
                defaultSeverity: defaultSeverity,
                isEnabledByDefault: isEnabledByDefault,
                description: description,
                helpLinkUri: $"https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/{id}.md",
                customTags: customTags);
        }
    }
}
