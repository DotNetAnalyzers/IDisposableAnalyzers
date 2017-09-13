namespace IDisposableAnalyzers.Test
{
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class NestedCodeFixVerifier<T>
        where T : CodeFixVerifier, new()
    {
        private readonly T parent = new T();

        public DiagnosticResult CSharpDiagnostic()
        {
            return this.parent.CSharpDiagnostic();
        }

        public Task VerifyCSharpDiagnosticAsync(string testCode, DiagnosticResult expected)
        {
            return this.parent.VerifyCSharpDiagnosticAsync(testCode, expected);
        }

        public Task VerifyCSharpDiagnosticAsync(string[] testCode, DiagnosticResult expected)
        {
            return this.parent.VerifyCSharpDiagnosticAsync(testCode, expected);
        }

        public Task VerifyCSharpDiagnosticAsync(string[] testCode, DiagnosticResult[] expected)
        {
            return this.parent.VerifyCSharpDiagnosticAsync(testCode, expected);
        }

        /// <summary>
        /// Called to test a C# code fix when applied on the input source as a string.
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the code fix was applied to it.</param>
        /// <param name="newSource">A class in the form of a string after the code fix was applied to it.</param>
        /// <param name="batchNewSource">A class in the form of a string after the batch fixer was applied to it.</param>
        /// <param name="codeFixIndex">Index determining which code fix to apply if there are multiple.</param>
        /// <param name="allowNewCompilerDiagnostics">A value indicating whether or not the test will fail if the code fix introduces other warnings after being applied.</param>
        /// <param name="numberOfIncrementalIterations">The number of iterations the incremental fixer will be called.
        /// If this value is less than 0, the negated value is treated as an upper limit as opposed to an exact
        /// value.</param>
        /// <param name="numberOfFixAllIterations">The number of iterations the Fix All fixer will be called. If this
        /// value is less than 0, the negated value is treated as an upper limit as opposed to an exact value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task VerifyCSharpFixAsync(
            string oldSource,
            string newSource,
            string batchNewSource = null,
            int? codeFixIndex = null,
            bool allowNewCompilerDiagnostics = false,
            int numberOfIncrementalIterations = 1,
            int numberOfFixAllIterations = 1,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.parent.VerifyCSharpFixAsync(
                oldSource: oldSource,
                newSource: newSource,
                batchNewSource: batchNewSource,
                codeFixIndex: codeFixIndex,
                allowNewCompilerDiagnostics: allowNewCompilerDiagnostics,
                numberOfIncrementalIterations: numberOfIncrementalIterations,
                numberOfFixAllIterations: numberOfFixAllIterations,
                cancellationToken: cancellationToken);
        }

        public Task VerifyCSharpFixAsync(
            string[] oldSource,
            string[] newSource,
            string[] batchNewSource = null,
            int? codeFixIndex = null,
            bool allowNewCompilerDiagnostics = false,
            int numberOfIncrementalIterations = 1,
            int numberOfFixAllIterations = 1,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.parent.VerifyCSharpFixAsync(
                oldSources: oldSource,
                newSources: newSource,
                batchNewSources: batchNewSource,
                codeFixIndex: codeFixIndex,
                allowNewCompilerDiagnostics: allowNewCompilerDiagnostics,
                numberOfIncrementalIterations: numberOfIncrementalIterations,
                numberOfFixAllIterations: numberOfFixAllIterations,
                cancellationToken: cancellationToken);
        }
    }
}