using Sample.Sdk.Core.Azure;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public interface IExternalServiceKeyProvider
    {
        Task<byte[]> GetExternalPublicKey(string externalWellknownEndpoint, HttpClient httpClient, AzureKeyVaultOptions options, CancellationToken token);
    }
}