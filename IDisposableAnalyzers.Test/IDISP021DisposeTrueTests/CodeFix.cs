﻿namespace IDisposableAnalyzers.Test.IDISP021DisposeTrueTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP021DisposeTrue);
        private static readonly CodeFixProvider Fix = new ArgumentFix();

        [Test]
        public static void WhenVirtual()
        {
            var before = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(↓false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenPrivate()
        {
            var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(↓false);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
