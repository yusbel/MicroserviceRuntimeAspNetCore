using Sample.Sdk.Core.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class ExternalServiceKeyProvider : IExternalServiceKeyProvider
    {
        public async Task<byte[]> GetExternalPublicKey(string externalWellknownEndpoint
            , HttpClient httpClient
            , AzureKeyVaultOptions options
            , CancellationToken token)
        {
            try
            {
                var response = await httpClient.GetAsync($"{externalWellknownEndpoint}?action=publickey", token);
                var publicKeyWrapper = System.Text.Json.JsonSerializer.Deserialize<PublicKeyWrapper>(await response.Content.ReadAsStringAsync());
                var certificate = new X509Certificate2(Convert.FromBase64String(publicKeyWrapper.PublicKey));
                return Convert.FromBase64String(publicKeyWrapper.PublicKey);
            }
            catch (Exception e)
            {
                throw;
            }

        }
    }
}
