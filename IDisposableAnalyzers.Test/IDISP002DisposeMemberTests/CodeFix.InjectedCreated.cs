namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class InjectedCreated
        {
            [Test]
            public void CtorPassingCreatedIntoPrivateCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void FieldAssignedWithFactoryPassingCreatedIntoPrivateCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable disposable;

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static Foo Create()
        {
            return new Foo(new Disposable());
        }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static Foo Create()
        {
            return new Foo(new Disposable());
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void FieldAssignedWithExtensionMethodFactoryAssigningInCtor()
            {
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Factory
    {
        public static IDisposable AsDisposable(this object value)
        {
            return new Disposable();
        }
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public Foo(object value)
        {
            this.disposable = value.AsDisposable();
        }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(object value)
        {
            this.disposable = value.AsDisposable();
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, factoryCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, factoryCode, testCode }, fixedCode);
            }

            [Test]
            public void FieldAssignedWithGenericExtensionMethodFactoryAssigningInCtor()
            {
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Factory
    {
        public static IDisposable AsDisposable<T>(this T value)
        {
            return new Disposable();
        }
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public Foo(object value)
        {
            this.disposable = value.AsDisposable();
        }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(object value)
        {
            this.disposable = value.AsDisposable();
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, factoryCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(new[] { DisposableCode, factoryCode, testCode }, fixedCode);
            }

            [Test]
            public void FieldAssignedWithInjectedListOfIntGetEnumeratorInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo : IDisposable
    {
        ↓private readonly IEnumerator<int> current;

        public Foo(List<int> list)
        {
            this.current = list.GetEnumerator();
        }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo : IDisposable
    {
        private readonly IEnumerator<int> current;

        public Foo(List<int> list)
        {
            this.current = list.GetEnumerator();
        }

        public void Dispose()
        {
            this.current?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<FieldDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }
        }
    }
}