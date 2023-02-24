using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public interface IPointToPointChannel   
    {
        Task<(bool wasCreated, PointToPointChannel? channel, EncryptionDecryptionFail reason)> 
            Create(string identifier
            , string externalWellKnownEndpoint
            , CertificateClient certificateClient
            , AzureKeyVaultOptions options
            , HttpClient httpClient
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , ILoggerFactory loggerFactory
            , CancellationToken token);
        Task<(bool wasDecrypted, byte[]? content, EncryptionDecryptionFail reason)> DecryptContent(string externalWellknownEndpoint
            , byte[] encryptedData
            , IHttpClientResponseConverter httpClientResponseConverter
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token);
    }
}