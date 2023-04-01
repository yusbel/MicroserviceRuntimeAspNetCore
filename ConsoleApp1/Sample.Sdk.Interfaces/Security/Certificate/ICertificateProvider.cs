using Azure.Security.KeyVault.Certificates;
using Sample.Sdk.Data.Enums;
using System.Security.Cryptography.X509Certificates;

namespace Sample.Sdk.Interface.Security.Certificate
{
    /// <summary>
    /// Internal use only
    /// </summary>
    public interface ICertificateProvider
    {
        Task<(bool? WasDownloaded, X509Certificate2? Certificate)>
            DownloadCertificate(string certificateName, Enums.HostTypeOptions keyVaultType, CancellationToken token, string? version = null);


        Task<(bool? WasDownloaded, KeyVaultCertificateWithPolicy? CertificateWithPolicy)>
            GetCertificate(string certificateName, Enums.HostTypeOptions keyVaultType, CancellationToken token);
    }
}
