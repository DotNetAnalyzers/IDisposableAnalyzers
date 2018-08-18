namespace ValidCode
{
    using System;
    using System.IO;

    public class Using
    {
        public Using()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }

        public void UsingDeclarationNewDisposable()
        {
            using (var disposable = new Disposable())
            {
            }
        }

        public void UsingDeclarationFileOpenRead()
        {
            using (var disposable = File.OpenRead(string.Empty))
            {
            }
        }


        public void UsingDeclarationObservableSubscribe(IObservable<int> observable)
        {
            using (var disposable = observable.Subscribe(x => Console.WriteLine(x)))
            {
            }
        }

        public void DeclareBeforeAssign()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }

        public void UsingNewDisposable()
        {
            using (new Disposable())
            {
            }
        }

        public void UsingFileOpenRead()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }

        public void UsingObservableSubscribe(IObservable<int> observable)
        {
            using (observable.Subscribe(x=> Console.WriteLine(x)))
            {
            }
        }
    }
}