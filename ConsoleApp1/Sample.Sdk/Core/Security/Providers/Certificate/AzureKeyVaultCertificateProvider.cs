﻿using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Certificate.Interfaces;
using Sample.Sdk.InMemory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Certificate
{
    /// <summary>
    /// Certificate provider use <see cref="CertificateClient"/> to download certificate from azure key vault.
    /// </summary>
    /// <remarks>
    /// Use memory cache to store the certificate with absolute experitation from download time.
    /// </remarks>
    public class AzureKeyVaultCertificateProvider : ICertificateProvider
    {
        private readonly IMemoryCacheState<string, X509Certificate2> _certificates;
        private readonly List<KeyValuePair<Enums.Enums.AzureKeyVaultOptionsType, CertificateClient>> _certificateClients;
        private readonly IMemoryCacheState<string, KeyVaultCertificateWithPolicy> _certificatesWithPolicy;

        public AzureKeyVaultCertificateProvider(IMemoryCacheState<string, X509Certificate2> certificates,
            List<KeyValuePair<Enums.Enums.AzureKeyVaultOptionsType, CertificateClient>> certificateClients,
            IMemoryCacheState<string, KeyVaultCertificateWithPolicy> certificatesWithPolicy)
        {
            Guard.ThrowWhenNull(certificates, certificateClients);
            _certificates = certificates;
            _certificateClients = certificateClients;
            _certificatesWithPolicy = certificatesWithPolicy;
        }

        /// <summary>
        /// Download a <see cref="X509Certificate2"/> from azure key vault
        /// </summary>
        /// <param name="certificateName">Certificate name to be downlaoded</param>
        /// <param name="token">A <see cref="CancellationToken"/> stopping request</param>
        /// <param name="version">Optional version of the certificate</param>
        /// <returns>A <see cref="Boolean"/> is certificate was downloaded. A <see cref="X509Certificate2"/> downloaded</returns>
        /// <exception cref="ArgumentNullException">when certificate name is null or empty</exception>
        /// <exception cref="InvalidDataException">the secret do not contain a certificate</exception>
        /// <exception cref="NotSupportedException">the certificate type is not supported</exception>
        /// <exception cref="PlatformNotSupportedException">Can not create this certificate</exception>
        /// <exception cref="RequestFailedException">See error code</exception>
        public async Task<(bool? WasDownloaded, X509Certificate2? Certificate)>
            DownloadCertificate(string certificateName, Enums.Enums.AzureKeyVaultOptionsType keyVaultType, CancellationToken token, string? version = null)
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
            GetCertificate(string certificateName, Enums.Enums.AzureKeyVaultOptionsType keyVaultType, CancellationToken token)
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

        private string GetCertificateId(string certificateName, Enums.Enums.AzureKeyVaultOptionsType azureKeyVaultType) 
        {
            return $"{certificateName}{azureKeyVaultType}";
        }

        private CertificateClient GetCertificateClient(Enums.Enums.AzureKeyVaultOptionsType azureKeyVaultOptionsType) 
        {
            return _certificateClients.Where(client => client.Key == azureKeyVaultOptionsType)
                                        .Select(item => item.Value)
                                        .First();
        }
    }
}
