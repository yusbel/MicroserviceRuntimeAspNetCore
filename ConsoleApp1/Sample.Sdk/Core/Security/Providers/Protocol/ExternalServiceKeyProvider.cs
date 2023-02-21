using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
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
        private readonly ILogger<ExternalServiceKeyProvider> _logger;

        public ExternalServiceKeyProvider(ILogger<ExternalServiceKeyProvider> logger) 
        {
            _logger = logger;
        }
        public async Task<(bool wasRetrieved, byte[]? publicKey, EncryptionDecryptionFail reason)> GetExternalPublicKey(string externalWellknownEndpoint
            , HttpClient httpClient
            , AzureKeyVaultOptions options
            , CancellationToken token)
        {
            HttpResponseMessage responseMessage;
            try
            {
                responseMessage = await httpClient.GetAsync($"{externalWellknownEndpoint}?action=publickey", token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when retrievign public key");
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPublicKey);
            }
            try
            {
                var publicKeyWrapper = System.Text.Json.JsonSerializer.Deserialize<PublicKeyWrapper>(await responseMessage.Content.ReadAsStringAsync());
                var certificate = new X509Certificate2(Convert.FromBase64String(publicKeyWrapper.PublicKey));
                return (true, Convert.FromBase64String(publicKeyWrapper.PublicKey), EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error occurred when retrieving the public key");
                return (false, default(byte[]?), EncryptionDecryptionFail.InValidPublicKey);
            }
        }
    }
}
