// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    /// <summary>
    /// Superclass of all unit tests made for diagnostics with code fixes.
    /// Contains methods used to verify correctness of code fixes.
    /// </summary>
    public abstract partial class CodeFixVerifier : DiagnosticVerifier
    {
        private const int DefaultNumberOfIncrementalIterations = -1000;

        [Test]
        public void NameMatchesExportedName()
        {
            CodeFixAssert.NameMatchesExportedName(this.GetCSharpCodeFixProvider());
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
        public Task VerifyCSharpFixAsync(string oldSource, string newSource, string batchNewSource = null, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, int numberOfIncrementalIterations = DefaultNumberOfIncrementalIterations, int numberOfFixAllIterations = 1, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.VerifyCSharpFixAsync(new[] { oldSource }, new[] { newSource }, batchNewSource == null ? null : new[] { batchNewSource }, codeFixIndex, allowNewCompilerDiagnostics, numberOfIncrementalIterations, numberOfFixAllIterations, cancellationToken);
        }

        /// <summary>
        /// Called to test a C# code fix when applied on the input source as a string.
        /// </summary>
        /// <param name="oldSources">An array of sources in the form of strings before the code fix was applied to them.</param>
        /// <param name="newSources">An array of sources in the form of strings after the code fix was applied to them.</param>
        /// <param name="batchNewSources">An array of sources in the form of a strings after the batch fixer was applied to them.</param>
        /// <param name="codeFixIndex">Index determining which code fix to apply if there are multiple.</param>
        /// <param name="allowNewCompilerDiagnostics">A value indicating whether or not the test will fail if the code fix introduces other warnings after being applied.</param>
        /// <param name="numberOfIncrementalIterations">The number of iterations the incremental fixer will be called.
        /// If this value is less than 0, the negated value is treated as an upper limit as opposed to an exact
        /// value.</param>
        /// <param name="numberOfFixAllIterations">The number of iterations the Fix All fixer will be called. If this
        /// value is less than 0, the negated value is treated as an upper limit as opposed to an exact value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task VerifyCSharpFixAsync(string[] oldSources, string[] newSources, string[] batchNewSources = null, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, int numberOfIncrementalIterations = DefaultNumberOfIncrementalIterations, int numberOfFixAllIterations = 1, CancellationToken cancellationToken = default(CancellationToken))
        {
            var t1 = this.VerifyFixInternalAsync(this.GetCSharpDiagnosticAnalyzers().ToImmutableArray(), this.GetCSharpCodeFixProvider(), oldSources, newSources, codeFixIndex, allowNewCompilerDiagnostics, numberOfIncrementalIterations, FixEachAnalyzerDiagnosticAsync, cancellationToken).ConfigureAwait(false);

            var fixAllProvider = this.GetCSharpCodeFixProvider().GetFixAllProvider();
            ////Assert.AreNotEqual(WellKnownFixAllProviders.BatchFixer, fixAllProvider);

            if (fixAllProvider == null)
            {
                await t1;
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    await t1;
                }

                var t2 = fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Document)
                    ? this.VerifyFixInternalAsync(this.GetCSharpDiagnosticAnalyzers().ToImmutableArray(), this.GetCSharpCodeFixProvider(), oldSources, batchNewSources ?? newSources, codeFixIndex, allowNewCompilerDiagnostics, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInDocumentAsync, cancellationToken).ConfigureAwait(false)
                    : FinishedTasks.Task.ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t2;
                }

                var t3 = fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Project)
                    ? this.VerifyFixInternalAsync(this.GetCSharpDiagnosticAnalyzers().ToImmutableArray(), this.GetCSharpCodeFixProvider(), oldSources, batchNewSources ?? newSources, codeFixIndex, allowNewCompilerDiagnostics, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInProjectAsync, cancellationToken).ConfigureAwait(false)
                    : FinishedTasks.Task.ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t3;
                }

                var t4 = fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Solution)
                    ? this.VerifyFixInternalAsync(this.GetCSharpDiagnosticAnalyzers().ToImmutableArray(), this.GetCSharpCodeFixProvider(), oldSources, batchNewSources ?? newSources, codeFixIndex, allowNewCompilerDiagnostics, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInSolutionAsync, cancellationToken).ConfigureAwait(false)
                    : FinishedTasks.Task.ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t4;
                }

                if (!Debugger.IsAttached)
                {
                    // Allow the operations to run in parallel
                    await t1;
                    await t2;
                    await t3;
                    await t4;
                }
            }
        }

        /// <summary>
        /// Called to test a C# fix all provider when applied on the input source as a string.
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the code fix was applied to it.</param>
        /// <param name="newSource">A class in the form of a string after the code fix was applied to it.</param>
        /// <param name="codeFixIndex">Index determining which code fix to apply if there are multiple.</param>
        /// <param name="allowNewCompilerDiagnostics">A value indicating whether or not the test will fail if the code fix introduces other warnings after being applied.</param>
        /// <param name="numberOfIterations">The number of iterations the fixer will be called. If this value is less
        /// than 0, the negated value is treated as an upper limit as opposed to an exact value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task VerifyCSharpFixAllFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, int numberOfIterations = 1, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.VerifyFixInternalAsync(this.GetCSharpDiagnosticAnalyzers().ToImmutableArray(), this.GetCSharpCodeFixProvider(), new[] { oldSource }, new[] { newSource }, codeFixIndex, allowNewCompilerDiagnostics, numberOfIterations, FixAllAnalyzerDiagnosticsInDocumentAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the code fix being tested (C#) - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeFixProvider"/> to be used for C# code.</returns>
        protected abstract CodeFixProvider GetCSharpCodeFixProvider();

        /// <summary>
        /// Gets all offered code fixes for the specified diagnostic within the given source.
        /// </summary>
        /// <param name="source">A valid C# source file in the form of a string.</param>
        /// <param name="diagnosticIndex">Index determining which diagnostic to use for determining the offered code fixes. Uses the first diagnostic if null.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>The collection of offered code actions. This collection may be empty.</returns>
        protected async Task<ImmutableArray<CodeAction>> GetOfferedCSharpFixesAsync(string source, int? diagnosticIndex = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.GetOfferedFixesInternalAsync(source, diagnosticIndex, this.GetCSharpDiagnosticAnalyzers().ToImmutableArray(), this.GetCSharpCodeFixProvider(), cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Project> FixEachAnalyzerDiagnosticAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzers, project.Documents.ToArray(), cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                if (--numberOfIterations < 0)
                {
                    return project;
                    ////Assert.Fail("The upper limit for the number of code fix iterations was exceeded");
                }

                previousDiagnostics = analyzerDiagnostics;

                done = true;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                    {
                        // do not pass unsupported diagnostics to a code fix provider
                        continue;
                    }

                    var actions = new List<CodeAction>();
                    var context = new CodeFixContext(project.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => actions.Add(a), cancellationToken);
                    await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                    if (actions.Count > 0)
                    {
                        var fixedProject = await ApplyFixAsync(project, actions.ElementAt(codeFixIndex.GetValueOrDefault(0)), cancellationToken).ConfigureAwait(false);
                        if (fixedProject != project)
                        {
                            done = false;

                            project = await RecreateProjectDocumentsAsync(fixedProject, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }
                }
            }
            while (!done);

            if (expectedNumberOfIterations >= 0)
            {
                Assert.AreEqual($"{expectedNumberOfIterations} iterations", $"{expectedNumberOfIterations - numberOfIterations} iterations");
            }

            return project;
        }

        private static Task<Project> FixAllAnalyzerDiagnosticsInDocumentAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Document, analyzers, codeFixProvider, codeFixIndex, project, numberOfIterations, cancellationToken);
        }

        private static Task<Project> FixAllAnalyzerDiagnosticsInProjectAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Project, analyzers, codeFixProvider, codeFixIndex, project, numberOfIterations, cancellationToken);
        }

        private static Task<Project> FixAllAnalyzerDiagnosticsInSolutionAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Solution, analyzers, codeFixProvider, codeFixIndex, project, numberOfIterations, cancellationToken);
        }

        private static async Task<Project> FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope scope, ImmutableArray<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            var fixAllProvider = codeFixProvider.GetFixAllProvider();

            if (fixAllProvider == null)
            {
                return null;
            }

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzers, project.Documents.ToArray(), cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                if (--numberOfIterations < 0)
                {
                    Assert.Fail("The upper limit for the number of fix all iterations was exceeded");
                }

                Diagnostic firstDiagnostic = null;
                string equivalenceKey = null;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                    {
                        // do not pass unsupported diagnostics to a code fix provider
                        continue;
                    }

                    var actions = new List<CodeAction>();
                    var context = new CodeFixContext(project.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => actions.Add(a), cancellationToken);
                    await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                    if (actions.Count > (codeFixIndex ?? 0))
                    {
                        firstDiagnostic = diagnostic;
                        equivalenceKey = actions[codeFixIndex ?? 0].EquivalenceKey;
                        break;
                    }
                }

                if (firstDiagnostic == null)
                {
                    return project;
                }

                previousDiagnostics = analyzerDiagnostics;

                done = true;

                FixAllContext.DiagnosticProvider fixAllDiagnosticProvider = TestDiagnosticProvider.Create(analyzerDiagnostics);

                var analyzerDiagnosticIds = analyzers.SelectMany(x => x.SupportedDiagnostics).Select(x => x.Id);
                var compilerDiagnosticIds = codeFixProvider.FixableDiagnosticIds.Where(x => x.StartsWith("CS", StringComparison.Ordinal));
                var disabledDiagnosticIds = project.CompilationOptions.SpecificDiagnosticOptions.Where(x => x.Value == ReportDiagnostic.Suppress).Select(x => x.Key);
                var relevantIds = analyzerDiagnosticIds.Concat(compilerDiagnosticIds).Except(disabledDiagnosticIds).Distinct();
                var fixAllContext = new FixAllContext(project.GetDocument(firstDiagnostic.Location.SourceTree), codeFixProvider, scope, equivalenceKey, relevantIds, fixAllDiagnosticProvider, cancellationToken);

                var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
                if (action == null)
                {
                    return project;
                }

                var fixedProject = await ApplyFixAsync(project, action, cancellationToken).ConfigureAwait(false);
                if (fixedProject != project)
                {
                    done = false;

                    project = await RecreateProjectDocumentsAsync(fixedProject, cancellationToken).ConfigureAwait(false);
                }
            }
            while (!done);

            if (expectedNumberOfIterations >= 0)
            {
                Assert.AreEqual($"{expectedNumberOfIterations} iterations", $"{expectedNumberOfIterations - numberOfIterations} iterations");
            }

            return project;
        }

        private static bool AreDiagnosticsDifferent(ImmutableArray<Diagnostic> analyzerDiagnostics, ImmutableArray<Diagnostic> previousDiagnostics)
        {
            if (analyzerDiagnostics.Length != previousDiagnostics.Length)
            {
                return true;
            }

            for (var i = 0; i < analyzerDiagnostics.Length; i++)
            {
                if ((analyzerDiagnostics[i].Id != previousDiagnostics[i].Id)
                    || (analyzerDiagnostics[i].Location.SourceSpan != previousDiagnostics[i].Location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task VerifyFixInternalAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            CodeFixProvider codeFixProvider,
            string[] oldSources,
            string[] newSources,
            int? codeFixIndex,
            bool allowNewCompilerDiagnostics,
            int numberOfIterations,
            Func<ImmutableArray<DiagnosticAnalyzer>, CodeFixProvider, int?, Project, int, CancellationToken, Task<Project>> getFixedProject,
            CancellationToken cancellationToken)
        {
            var project = CodeFactory.CreateProject(oldSources, analyzers);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

            project = await getFixedProject(analyzers, codeFixProvider, codeFixIndex, project, numberOfIterations, cancellationToken).ConfigureAwait(false);

            var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false));

            // Check if applying the code fix introduced any new compiler diagnostics
            if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
            {
                // Format and get the compiler diagnostics again so that the locations make sense in the output
                project = await ReformatProjectDocumentsAsync(project, cancellationToken).ConfigureAwait(false);
                newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false));

                var message = new StringBuilder();
                message.Append("Fix introduced new compiler diagnostics:\r\n");
                newCompilerDiagnostics.Aggregate(message, (sb, d) => sb.Append(d.ToString()).Append("\r\n")).IgnoreReturnValue();
                foreach (var document in project.Documents)
                {
                    message.Append("\r\n").Append(document.Name).Append(":\r\n");
                    message.Append((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).ToFullString());
                    message.Append("\r\n");
                }

                Assert.Fail(message.ToString());
            }

            // After applying all of the code fixes, compare the resulting string to the inputted one
            var updatedDocuments = project.Documents.ToArray();

            Assert.AreEqual($"{newSources.Length} documents", $"{updatedDocuments.Length} documents");

            for (var i = 0; i < updatedDocuments.Length; i++)
            {
                var actual = await GetStringFromDocumentAsync(updatedDocuments[i], cancellationToken).ConfigureAwait(false);
                var expectedCode = new CodeReader(newSources[i]);
                var actualCode = new CodeReader(actual);
                if (expectedCode != actualCode)
                {
                    Console.WriteLine("Expected:");
                    Console.Write(expectedCode);
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Actual:");
                    Console.Write(actualCode);
                    Console.WriteLine();
                }

                Assert.AreEqual(expectedCode, actualCode);
            }
        }

        private async Task<ImmutableArray<CodeAction>> GetOfferedFixesInternalAsync(string source, int? diagnosticIndex, ImmutableArray<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, CancellationToken cancellationToken)
        {
            var document = CodeFactory.CreateDocument(source, analyzers);
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzers, new[] { document }, cancellationToken).ConfigureAwait(false);

            var index = diagnosticIndex ?? 0;

            Assert.True(index < analyzerDiagnostics.Count());

            var actions = new List<CodeAction>();

            // do not pass unsupported diagnostics to a code fix provider
            if (codeFixProvider.FixableDiagnosticIds.Contains(analyzerDiagnostics[index].Id))
            {
                var context = new CodeFixContext(document, analyzerDiagnostics[index], (a, d) => actions.Add(a), cancellationToken);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
            }

            return actions.ToImmutableArray();
        }
    }
}
