using Newtonsoft.Json;
using Sample.Sdk.Core.Security.Providers.Protocol;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sample.Employee.Tests
{
    [TestClass]
    public class WellknownMiddlewareTests
    {
        private const string wellknownEndpoint = "http://localhost:5500/Wellknown";

        [TestMethod]
        public async Task GivenAValidHttpRequestThenReturnPublicKey()
        {
            var httpClient = new HttpClient();
            var result = await httpClient.GetAsync(new Uri($"{wellknownEndpoint}?action=publickey"));
            var publicKeyWrapper = System.Text.Json.JsonSerializer.Deserialize<PublicKeyWrapper>(await result.Content.ReadAsByteArrayAsync());
            var certificate = new X509Certificate2(Convert.FromBase64String(publicKeyWrapper.PublicKey));
            Assert.IsTrue(certificate != null && certificate.GetRSAPublicKey() != null);
        }

        [TestMethod]
        public async Task GivenPublicKeyThenCreateSessionAndReturnSessionIdentifierEncrypted() 
        {
            

        }
    }
}