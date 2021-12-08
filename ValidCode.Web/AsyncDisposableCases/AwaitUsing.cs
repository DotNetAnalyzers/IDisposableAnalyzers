namespace ValidCode.Web.AsyncDisposableCases
{
    using System.Threading.Tasks;

    class AwaitUsing
    {
        public async void M1()
        {
            await using var impl1 = new Impl1();
            await using var impl2 = new Impl2();
        }

        public async Task M2()
        {
            await using var impl1 = new Impl1();
            await using var impl2 = new Impl2();
        }
    }
}
