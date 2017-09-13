namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposable>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [Test]
        public async Task IgnoringFileOpenRead()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        ↓File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringNewDisposable()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
public sealed class Foo
{
    public void Meh()
    {
        ↓new Disposable();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task FactoryMethodNewDisposable()
        {
            var disposableCode = @"
using System;

class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
public sealed class Foo
{
    public void Meh()
    {
        ↓Create();
    }

    private static Disposable Create()
    {
        return new Disposable();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringFileOpenReadPassedIntoCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Bar
{
    private readonly Stream stream;

    public Bar(Stream stream)
    {
       this.stream = stream;
    }
}

public sealed class Foo
{
    public Bar Meh()
    {
        return new Bar(↓File.OpenRead(string.Empty));
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringNewDisposabledPassedIntoCtor()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";
            var barCode = @"
using System;

public class Bar
{
    private readonly IDisposable disposable;

    public Bar(IDisposable disposable)
    {
       this.disposable = disposable;
    }
}";

            var testCode = @"
public sealed class Foo
{
    public Bar Meh()
    {
        return new Bar(↓new Disposable());
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, barCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task Generic()
        {
            var interfaceCode = @"
    using System;
    public interface IDisposable<T> : IDisposable
    {
    }";

            var disposableCode = @"
    public sealed class Disposable<T> : IDisposable<T>
    {
        public void Dispose()
        {
        }
    }";

            var factoryCode = @"
    public class Factory
    {
        public static IDisposable<T> Create<T>() => new Disposable<T>();
    }";

            var testCode = @"
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<int>();
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { interfaceCode, disposableCode, factoryCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ConstrainedGeneric()
        {
            var factoryCode = @"
using System;

public class Factory
{
    public static T Create<T>() where T : IDisposable, new() => new T();
}";

            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<Disposable>();
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { factoryCode, DisposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task WithOptionalParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            ↓Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, List<IDisposable> list = null)
        {
            if (list == null)
            {
                list = new List<IDisposable>();
            }

            if (list.Contains(disposable))
            {
                return new Disposable();
            }

            list.Add(disposable);
            return Bar(disposable, list);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningNewAssigningNotDisposing()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(↓new Disposable());
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, fooCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningNewNotAssigning()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        public Foo(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(↓new Disposable());
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, fooCode, testCode }, expected).ConfigureAwait(false);
        }
    }
}