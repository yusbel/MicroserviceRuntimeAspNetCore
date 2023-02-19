using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public interface IPointToPointChannel   
    {
        Task<PointToPointChannel> Create(string identifier
            , string externalWellKnownEndpoint
            , CertificateClient certificateClient
            , AzureKeyVaultOptions options
            , HttpClient httpClient
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , ILoggerFactory loggerFactory
            , CancellationToken token);
        Task<byte[]> DecryptContent(string externalWellknownEndpoint
            , byte[] encryptedData
            , HttpClient httpClient
            , IAsymetricCryptoProvider cryptoProvider);
    }
}