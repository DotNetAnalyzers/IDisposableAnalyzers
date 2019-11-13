namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Repro
        {
            [Test]
            public static void Issue63()
            {
                var viewModelBaseCode = @"
namespace MVVM
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;

    using System.Runtime.CompilerServices;

    /// <summary>
    /// Base class for all ViewModel classes in the application.
    /// It provides support for property change notifications
    /// and has a DisplayName property. This class is abstract.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        private readonly object disposeLock = new object();
        private bool isDisposed;

        protected ViewModelBase()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Dispose()
        {
            lock (disposeLock)
            {
                this.OnDispose();

                if (isDisposed)
                    return;

                foreach (var disposable in disposables)
                    disposable.Dispose();

                isDisposed = true;
            }
        }

        protected virtual void OnDispose()
        {
        }
    }
}";
                var popupViewModelCode = @"
namespace ProjectX.ViewModel
{
    using System;
    using MVVM;

    public class PopupViewModel : ViewModelBase
    {
        public PopupViewModel()
        {
            ClosePopupCommand = new ClosePopupCommand(this);
        }

        // Gives an IDISP006 warning (need to implement IDispose)
        public ClosePopupCommand ClosePopupCommand { get; }

        protected override void OnDispose()
        {
            ClosePopupCommand.Dispose();
            CloseProgramCommand.Dispose();
        }
    }
}";

                var closePopupCommandCode = @"
namespace ProjectX.Commands
{
    using System;

    public sealed class ClosePopupCommand : IDisposable
    {
        private readonly object disposeLock = new object();
        private bool isDisposed;
        private bool isBusy = false;

        internal ClosePopupCommand()
        {
        }

        public event EventHandler CanExecuteChanged;

        public void Dispose()
        {
            lock (disposeLock)
            {
                if (isDisposed)
                    return;

                // Here we have code that actually needs to be disposed off...

                isDisposed = true;
            }
        }
    }
}";

                var solution = CodeFactory.CreateSolution(
                    new[] { viewModelBaseCode, popupViewModelCode, closePopupCommandCode },
                    CodeFactory.DefaultCompilationOptions(Analyzer),
                    MetadataReferences.FromAttributes());
                RoslynAssert.NoDiagnostics(Analyze.GetDiagnostics(Analyzer, solution));
            }

            public static void Issue150()
            {
                var testCode = @"
namespace ValidCode
{
    using System.Collections.Generic;
    using System.IO;

    public class Issue150
    {
        public Issue150(string name)
        {
            this.Name = name;
            if (File.Exists(name))
            {
                this.AllText = File.ReadAllText(name);
                this.AllLines = File.ReadAllLines(name);
            }
        }

        public string Name { get; }

        public bool Exists => File.Exists(this.Name);

        public string AllText { get; }

        public IReadOnlyList<string> AllLines { get; }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
