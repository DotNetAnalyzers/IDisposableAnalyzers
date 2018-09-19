namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class HandlesRecursion
    {
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers = typeof(AnalyzerCategory)
                                                                                 .Assembly
                                                                                 .GetTypes()
                                                                                 .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                                                                 .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                                                                 .ToArray();

        [Test]
        public void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(AllAnalyzers);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void RecursiveSample(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable FunctionRecursiveOnAllPaths
// ReSharper disable UnusedParameter.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeThisQualifier
// ReSharper disable PossibleUnintendedReferenceComparison
// ReSharper disable RedundantCheckBeforeAssignment
// ReSharper disable UnusedMethodReturnValue.Global
#pragma warning disable 1717
#pragma warning disable SA1101 // Prefix local calls with this
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable GU0010 // Assigning same value.
#pragma warning disable IDE0009 // Member access should be qualified.
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.Collections.Generic;

    public class RecursiveFoo
    {
        private IDisposable bar1;
        private IDisposable bar2;

        public RecursiveFoo()
            : this()
        {
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            value = value;
        }

        public IDisposable RecursiveProperty => this.RecursiveProperty;

        public int Value1 => this.Value1;

        public int Value2 => Value2;

        public int Value3 => this.Value1;

        public IDisposable RecursiveExpressionBodyProperty => this.RecursiveExpressionBodyProperty;

        public IDisposable RecursiveStatementBodyProperty
        {
            get
            {
                return this.RecursiveStatementBodyProperty;
            }
        }

        public IDisposable Value4
        {
            get
            {
                return this.Value4;
            }

            set
            {
                if (value == this.Value4)
                {
                    return;
                }

                this.Value4 = value;
            }
        }

        public IDisposable Value5
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
            }
        }

        public IDisposable Value6
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
            }
        }

        public IDisposable Bar1
        {
            get
            {
                return this.bar1;
            }

            set
            {
                if (Equals(value, this.bar1))
                {
                    return;
                }

                if (value != null && this.bar2 != null)
                {
                    this.Bar2 = null;
                }

                this.bar1 = value;
            }
        }

        public IDisposable Bar2
        {
            get
            {
                return this.bar2;
            }

            set
            {
                if (Equals(value, this.bar2))
                {
                    return;
                }

                if (value != null && this.bar1 != null)
                {
                    this.Bar1 = null;
                }

                this.bar2 = value;
            }
        }

        public static bool RecursiveOut(out IDisposable value)
        {
            return RecursiveOut(out value);
        }

        public static bool RecursiveOut(int foo, out IDisposable value)
        {
            return RecursiveOut(2, out value);
        }

        public static bool RecursiveOut(double foo, out IDisposable value)
        {
            value = null;
            return RecursiveOut(3.0, out value);
        }

        public static bool RecursiveOut(string foo, out IDisposable value)
        {
            if (foo == null)
            {
                return RecursiveOut(3.0, out value);
            }

            value = null;
            return true;
        }

        public static bool RecursiveRef(ref IDisposable value)
        {
            return RecursiveRef(ref value);
        }

        public Disposable RecursiveMethod() => this.RecursiveMethod();

        public void NotUsingRecursive()
        {
            var item1 = this.RecursiveProperty;
            var item2 = this.RecursiveMethod();
        }

        public void UsingRecursive()
        {
            using (var item = new Disposable())
            {
            }

            using (var item = this.RecursiveProperty)
            {
            }

            using (this.RecursiveProperty)
            {
            }

            using (var item = this.RecursiveMethod())
            {
            }

            using (this.RecursiveMethod())
            {
            }
        }

        public IDisposable RecursiveExpressionBodyMethod() => this.RecursiveExpressionBodyMethod();

        public IDisposable RecursiveExpressionBodyMethod(int value) => this.RecursiveExpressionBodyMethod(value);

        public IDisposable RecursiveStatementBodyMethod()
        {
            return this.RecursiveStatementBodyMethod();
        }

        public IDisposable RecursiveStatementBodyMethod(int value)
        {
            return this.RecursiveStatementBodyMethod(value);
        }

        public void Meh()
        {
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            RecursiveOut(out value);
            RecursiveOut(1, out value);
            RecursiveOut(1.0, out value);
            RecursiveOut(string.Empty, out value);
            RecursiveRef(ref value);
            value = value;
        }

        private static IDisposable RecursiveStatementBodyMethodWithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return RecursiveStatementBodyMethodWithOptionalParameter(value, new[] { value });
            }

            return value;
        }

        private static IReadOnlyList<IDisposable> Flatten(IReadOnlyList<IDisposable> source, List<IDisposable> result = null)
        {
            result = result ?? new List<IDisposable>();
            result.AddRange(source);
            foreach (var condition in source)
            {
                Flatten(new[] { condition }, result);
            }

            return result;
        }
    }
}";
            var converterCode = @"
namespace RoslynSandbox
{
     using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    public class ValidationErrorToStringConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        public static readonly ValidationErrorToStringConverter Default = new ValidationErrorToStringConverter();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text;
            }

            if (value is ValidationResult result)
            {
                return this.Convert(result.ErrorContent, targetType, parameter, culture);
            }

            if (value is ValidationError error)
            {
                return this.Convert(error.ErrorContent, targetType, parameter, culture);
            }

            return value;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($""{this.GetType().Name} only supports one-way conversion."");
        }
    }
}";
            AnalyzerAssert.Valid(analyzer, testCode, converterCode);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task InSetAndRaise(DiagnosticAnalyzer analyzer)
        {
            var viewModelBaseCode = @"
namespace RoslynSandbox.Core
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool TrySet<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            return this.TrySet(ref field, newValue, propertyName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox.Client
{
    public class Foo : RoslynSandbox.Core.ViewModelBase
    {
        private int value2;

        public int Value1 { get; set; }

        public int Value2
        {
            get { return this.value2; }
            set { this.TrySet(ref this.value2, value); }
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { viewModelBaseCode, testCode }, AnalyzerAssert.MetadataReferences).ConfigureAwait(false);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task InOnPropertyChanged(DiagnosticAnalyzer analyzer)
        {
            var viewModelBaseCode = @"
namespace RoslynSandbox.Core
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.OnPropertyChanged(propertyName);
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox.Client
{
    public class Foo : RoslynSandbox.Core.ViewModelBase
    {
        private int value2;

        public int Value1 { get; set; }

        public int Value2
        {
            get
            {
                return this.value2;
            }

            set
            {
                if (value == this.value2)
                {
                    return;
                }

                this.value2 = value;
                this.OnPropertyChanged();
            }
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { viewModelBaseCode, testCode }, AnalyzerAssert.MetadataReferences).ConfigureAwait(false);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task InProperty(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class Foo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Value1 => this.Value1;

        public int Value2 => Value2;

        public int Value3 => this.Value1;

        public int Value4
        {
            get
            {
                return this.Value4;
            }

            set
            {
                if (value == this.Value4)
                {
                    return;
                }

                this.Value4 = value;
                this.OnPropertyChanged();
            }
        }

        public int Value5
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
                this.OnPropertyChanged();
            }
        }

        public int Value6
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { testCode }, AnalyzerAssert.MetadataReferences).ConfigureAwait(false);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void Repro(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private Bar bar1;
        private Bar bar2;

        public Bar Bar1
        {
            get
            {
                return this.bar1;
            }

            set
            {
                if (Equals(value, this.bar1))
                {
                    return;
                }

                if (value != null && this.bar2 != null)
                {
                    this.Bar2 = null;
                }

                if (this.bar1 != null)
                {
                    this.bar1.Selected = false;
                }

                this.bar1 = value;
                if (this.bar1 != null)
                {
                    this.bar1.Selected = true;
                }
            }
        }

        public Bar Bar2
        {
            get
            {
                return this.bar2;
            }

            set
            {
                if (Equals(value, this.bar2))
                {
                    return;
                }

                if (value != null && this.bar1 != null)
                {
                    this.Bar1 = null;
                }

                if (this.bar2 != null)
                {
                    this.bar2.Selected = false;
                }

                this.bar2 = value;
                if (this.bar2 != null)
                {
                    this.bar2.Selected = true;
                }
            }
        }
    }

    public class Bar
    {
        public bool Selected { get; set; }
    }
}";
            AnalyzerAssert.Valid(analyzer, testCode);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task ConstructorCallingSelf(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Constructors : IDisposable
    {
        private IDisposable disposable;

        public Constructors()
            : this()
        {
        }

        public Constructors(IDisposable disposable)
            : this(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public Constructors(int i, IDisposable disposable)
            : this(disposable, i)
        {
            this.disposable = disposable;
        }

        public Constructors(IDisposable disposable, int i)
            : this(i, disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { testCode }, AnalyzerAssert.MetadataReferences).ConfigureAwait(false);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task ConstructorCycle(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Constructors : IDisposable
    {
        private IDisposable disposable;

        public Constructors(int i, IDisposable disposable)
            : this(disposable, i)
        {
            this.disposable = disposable;
        }

        public Constructors(IDisposable disposable, int i)
            : this(i, disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { testCode }, AnalyzerAssert.MetadataReferences).ConfigureAwait(false);
        }
    }
}
