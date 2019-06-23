namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class InjectedCreated
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly CodeFixProvider Fix = new DisposeMemberFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP002DisposeMember.Descriptor);

            [Test]
            public void CtorPassingCreatedIntoPrivateCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void FieldAssignedWithFactoryPassingCreatedIntoPrivateCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        private C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static C Create()
        {
            return new C(new Disposable());
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        private C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static C Create()
        {
            return new C(new Disposable());
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
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

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public C(object value)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(object value)
        {
            this.disposable = value.AsDisposable();
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, factoryCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, factoryCode, testCode }, fixedCode);
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

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public C(object value)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(object value)
        {
            this.disposable = value.AsDisposable();
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, factoryCode, testCode }, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, factoryCode, testCode }, fixedCode);
            }

            [Test]
            public void FieldAssignedWithInjectedListOfIntGetEnumeratorInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class C : IDisposable
    {
        ↓private readonly IEnumerator<int> current;

        public C(List<int> list)
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

    public class C : IDisposable
    {
        private readonly IEnumerator<int> current;

        public C(List<int> list)
        {
            this.current = list.GetEnumerator();
        }

        public void Dispose()
        {
            this.current?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
