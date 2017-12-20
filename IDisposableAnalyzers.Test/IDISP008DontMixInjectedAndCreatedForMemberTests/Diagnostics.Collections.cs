namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class Diagnostics
    {
        [Explicit("Fix later")]
        internal class Collections
        {
            [Test]
            public void ListInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo
    {
        private List<IDisposable> disposables;

        public void MethodName(IDisposable disposable)
        {
            ↓this.disposables = new List<IDisposable> { new Disposable(),  disposable };
        }
    }
}";
                AnalyzerAssert.Diagnostics(new FieldAndPropertyDeclarationAnalyzer(), DisposableCode, testCode);
            }
        }
    }
}
