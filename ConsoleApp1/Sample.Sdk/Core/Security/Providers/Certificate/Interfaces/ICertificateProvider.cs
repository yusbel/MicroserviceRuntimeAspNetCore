using Azure.Security.KeyVault.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Certificate.Interfaces
{
    /// <summary>
    /// Internal use only
    /// </summary>
    public interface ICertificateProvider
    {
        Task<(bool? WasDownloaded, X509Certificate2? Certificate)>
            DownloadCertificate(string certificateName, Enums.Enums.AzureKeyVaultOptionsType keyVaultType, CancellationToken token, string? version = null);


        Task<(bool? WasDownloaded, KeyVaultCertificateWithPolicy? CertificateWithPolicy)> 
            GetCertificate(string certificateName, Enums.Enums.AzureKeyVaultOptionsType keyVaultType, CancellationToken token);
    }
}
