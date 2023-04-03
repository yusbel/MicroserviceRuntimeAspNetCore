using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Caching;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Interface.Caching;
using Sample.Sdk.Interface.Security.Certificate;
using System.Security.Cryptography.X509Certificates;

namespace Sample.Sdk.Core.Security.Certificate
{
    /// <summary>
    /// Certificate provider use <see cref="CertificateClient"/> to download certificate from azure key vault.
    /// </summary>
    /// <remarks>
    /// Use memory cache to store the certificate with absolute experitation from download time.
    /// </remarks>
    public class AzureKeyVaultCertificateProvider : ICertificateProvider
    {
        private static Lazy<IMemoryCacheState<string, X509Certificate2>> certificates = new Lazy<IMemoryCacheState<string, X509Certificate2>>(
            ()=> 
            {
                return new MemoryCacheState<string, X509Certificate2>(new MemoryCache(Options.Create(new MemoryCacheOptions())));
            }, true);

        private static Lazy<IMemoryCacheState<string, KeyVaultCertificateWithPolicy>> certificateWithPolicy = new Lazy<IMemoryCacheState<string, KeyVaultCertificateWithPolicy>>(
            () => 
            {
                return new MemoryCacheState<string, KeyVaultCertificateWithPolicy>(new MemoryCache(Options.Create(new MemoryCacheOptions())));
            }, true);

        private readonly IMemoryCacheState<string, X509Certificate2> _certificates;
        private readonly IAzureClientFactory<CertificateClient> _certificateFactoryClient;
        private readonly IMemoryCacheState<string, KeyVaultCertificateWithPolicy> _certificatesWithPolicy;

        public AzureKeyVaultCertificateProvider(IAzureClientFactory<CertificateClient> certificateFactoryClient)
        {
            Guard.ThrowWhenNull(certificates);
            _certificates = certificates.Value;
            _certificateFactoryClient = certificateFactoryClient;
            _certificatesWithPolicy = certificateWithPolicy.Value;
        }

        /// <summary>
        /// Download a <see cref="X509Certificate2"/> from azure key vault
        /// </summary>
        /// <param name="certificateName">Certificate name to be downlaoded</param>
        /// <param name="token">A <see cref="CancellationToken"/> stopping request</param>
        /// <param name="version">Optional version of the certificate</param>
        /// <returns>A <see cref="bool"/> is certificate was downloaded. A <see cref="X509Certificate2"/> downloaded</returns>
        /// <exception cref="ArgumentNullException">when certificate name is null or empty</exception>
        /// <exception cref="InvalidDataException">the secret do not contain a certificate</exception>
        /// <exception cref="NotSupportedException">the certificate type is not supported</exception>
        /// <exception cref="PlatformNotSupportedException">Can not create this certificate</exception>
        /// <exception cref="RequestFailedException">See error code</exception>
        public async Task<(bool? WasDownloaded, X509Certificate2? Certificate)>
            DownloadCertificate(string certificateName, Enums.HostTypeOptions keyVaultType, CancellationToken token, string? version = null)
        {
            if (string.IsNullOrEmpty(certificateName))
            {
                throw new ArgumentNullException(nameof(certificateName));
            }
            var certificateId = $"Download{GetCertificateId(certificateName, keyVaultType)}";
            if (_certificates.Cache.TryGetValue(certificateId, out X509Certificate2 certificate))
            {
                return (true, certificate);
            }
            token.ThrowIfCancellationRequested();
            var certificateClient = GetCertificateClient(keyVaultType);
            try
            {
                certificate = await certificateClient.DownloadCertificateAsync(certificateName, version, token)
                                                        .ConfigureAwait(false);
                if (certificate != null)
                {
                    _certificates.Cache.Set(certificateId, certificate, TimeSpan.FromHours(5));
                    return (true, _certificates.Cache.Get<X509Certificate2>(certificateId));
                }
                return (false, default);
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// Get a <see cref="KeyVaultCertificateWithPolicy"/> from azure key vault
        /// </summary>
        /// <param name="certificateName">Certificate name</param>
        /// <param name="token">Cancel request</param>
        /// <returns><see cref="KeyVaultCertificateWithPolicy"/></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<(bool? WasDownloaded, KeyVaultCertificateWithPolicy? CertificateWithPolicy)>
            GetCertificate(string certificateName, Enums.HostTypeOptions keyVaultType, CancellationToken token)
        {
            if (string.IsNullOrEmpty(certificateName))
            {
                throw new ArgumentNullException($"{nameof(certificateName)}");
            }
            var certificateId = $"Get{GetCertificateId(certificateName, keyVaultType)}";
            var certificateClient = GetCertificateClient(keyVaultType);
            try
            {
                if (_certificatesWithPolicy.Cache.TryGetValue(certificateId, out KeyVaultCertificateWithPolicy certificate))
                {
                    return (true, certificate);
                }
                certificate = await certificateClient.GetCertificateAsync(certificateName, token).ConfigureAwait(false);
                if (certificate != null)
                {
                    _certificatesWithPolicy.Cache.Set(certificateId, certificate, TimeSpan.FromHours(5));
                    return (true, _certificatesWithPolicy.Cache.Get<KeyVaultCertificateWithPolicy>(certificateId));
                }
                else
                {
                    return (false, default);
                }
            }
            catch (Exception) { throw; }
        }

        private string GetCertificateId(string certificateName, Enums.HostTypeOptions azureKeyVaultType)
        {
            return $"{certificateName}{azureKeyVaultType}";
        }

        private CertificateClient GetCertificateClient(Enums.HostTypeOptions azureKeyVaultOptionsType)
        {
            return _certificateFactoryClient.CreateClient(azureKeyVaultOptionsType.ToString());
        }
    }
}
