namespace IDisposableAnalyzers.Test
{
    using System.Threading.Tasks;

    public abstract class NestedHappyPathVerifier<T>
        where T : IHappyPathVerifier, new()
    {
        private readonly T parent = new T();

        public Task VerifyHappyPathAsync(params string[] testCode)
        {
            return this.parent.VerifyHappyPathAsync(testCode);
        }
    }
}