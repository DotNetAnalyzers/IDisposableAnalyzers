namespace ValidCode.Web
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Issue242
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public Task<bool> TaskFromResultTrue()
        {
            return Task.FromResult(true);
        }

        public Task<MemoryStream> TaskTaskFromResultNewMemoryStream()
        {
            return Task.FromResult(new MemoryStream());
        }

        public Task<HttpResponseMessage> TaskTaskFromResultHttpResponseMessage()
        {
            return Task.FromResult(new HttpResponseMessage());
        }

        public async Task Run()
        {
            var taskFromResultTrue = this.TaskFromResultTrue(); // No IDISP001 = OK (maybe some Task not awaited warning but that is out of the scope of this package)
            var b1 = await taskFromResultTrue;
            var b2 = await taskFromResultTrue.ConfigureAwait(false);
            var b3 = await this.TaskFromResultTrue();
            var b4 = await this.TaskFromResultTrue().ConfigureAwait(true);
            
            var taskOfMemoryStream1 = this.TaskTaskFromResultNewMemoryStream(); // No IDISP001 = OK (maybe some Task not awaited warning but that is out of the scope of this package)
            using var memoryStream1 = await taskOfMemoryStream1;
            var taskOfMemoryStream2 = this.TaskTaskFromResultNewMemoryStream();
            using var memoryStream2 = await taskOfMemoryStream2.ConfigureAwait(true);
            using var memoryStream3 = await this.TaskTaskFromResultNewMemoryStream(); // IDISP001 Generated = OK
            using var memoryStream4 = await this.TaskTaskFromResultNewMemoryStream().ConfigureAwait(true);


            var taskTaskFromResultHttpResponseMessage1 = this.TaskTaskFromResultHttpResponseMessage(); // No IDISP001 = OK (maybe some Task not awaited warning but that is out of the scope of this package)
            using var httpResponseMessage1 = await taskTaskFromResultHttpResponseMessage1;
            var taskTaskFromResultHttpResponseMessage2 = this.TaskTaskFromResultHttpResponseMessage();
            using var httpResponseMessage2 = await taskTaskFromResultHttpResponseMessage2.ConfigureAwait(true);
            using var httpResponseMessage3 = await this.TaskTaskFromResultHttpResponseMessage(); // IDISP001 Generated = OK
            using var httpResponseMessage4 = await this.TaskTaskFromResultHttpResponseMessage().ConfigureAwait(true);

            // Correct and expected so far
            // Weird stuff is happening with this part using HttpClient
            // Note that GetAsync returns a task which resolves into a HttpResponseMessage which is a IDisposable
            // Note that this is not happening in case of Test3 above, which has the same signature as GetAsync
            var responseTask1 = HttpClient.GetAsync("http://example.com"); // Generates IDISP001, suggests using here
            using var response1 = await responseTask1; // IDISP001 should be here

            using var responseTask2 = HttpClient.GetAsync("http://example.com"); // using in front of this line removes IDISP001
            using var response2 = await responseTask2; // IDISP001 should actually still be here

            // The correct code is a 'using' in front of the variabele assigned the variabele.
            // In this case the IDISP001 is gone on both lines
            var responseTask3 = HttpClient.GetAsync("http://example.com");
            using var response3 = await responseTask3;
        }
    }
}
