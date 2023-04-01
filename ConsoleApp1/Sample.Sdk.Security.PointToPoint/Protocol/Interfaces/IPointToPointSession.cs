using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Azure;
using Sample.Sdk.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Security.Providers.Protocol.Http;
using Sample.Sdk.Security.Providers.Protocol.State;

namespace Sample.Sdk.Security.Providers.Protocol.Interfaces
{
    public interface IPointToPointSession
    {
        Task<(bool wasCreated, PointToPointSession? channel, EncryptionDecryptionFail reason)>
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