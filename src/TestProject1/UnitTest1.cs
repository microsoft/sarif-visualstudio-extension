using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            AuthManager authManager = new AuthManager();
            Microsoft.Identity.Client.AuthenticationResult x = await authManager.AuthenticateAsync();
            Console.WriteLine(x);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", x.AccessToken);
            HttpResponseMessage devCanvasOutput = await client.GetAsync("https://insightwebv2.azurewebsites.net/api/v1/SarifInsight/SarifInsightProviders");
            Console.WriteLine(devCanvasOutput);

        }
    }
}
