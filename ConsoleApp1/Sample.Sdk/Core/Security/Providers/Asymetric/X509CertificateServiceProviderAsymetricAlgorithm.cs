using Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.KeyVault.Cryptography.Algorithms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Certificate.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Asymetric
{
    /// <summary>
    /// Exceptions will be handler on the client code; the class will only handle exceptions that it can recover from
    /// TODO: Add memory cache
    /// </summary>
    public class X509CertificateServiceProviderAsymetricAlgorithm : IAsymetricCryptoProvider
    {
        private readonly IOptions<AzureKeyVaultOptions> _options;
        private readonly ILogger<X509CertificateServiceProviderAsymetricAlgorithm> _logger;
        private readonly ICertificateProvider _certificateProvider;

        public X509CertificateServiceProviderAsymetricAlgorithm(
            IOptions<AzureKeyVaultOptions> options
            , ILogger<X509CertificateServiceProviderAsymetricAlgorithm> logger
            , ICertificateProvider certificateProvider)
        {
            _options = options;
            _logger = logger;
            _certificateProvider = certificateProvider;
        }

        public async Task<(bool wasCreated, byte[]? data, EncryptionDecryptionFail reason)> 
            CreateSignature(
            byte[] baseString, 
            CancellationToken token)
        {
            X509Certificate2 certificate = null;
            try
            {
                var result = await _certificateProvider.DownloadCertificate(_options.Value.KeyVaultCertificateIdentifier, token).ConfigureAwait(false);
                if (result.WasDownloaded.HasValue && result.WasDownloaded.Value)
                {
                    certificate = result.Certificate!;
                }
                else 
                {
                    return (false, default, EncryptionDecryptionFail.UnableToGetCertificate);
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default(byte[]?), EncryptionDecryptionFail.UnableToGetCertificate);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.GetRSAPrivateKey();
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Unable to get private key {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            try
            {
                var signature = rsa?.SignData(baseString, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (true, signature, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Unable to create signature {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.SignatureCreationFail);
            }
        }

        /// <summary>
        /// Decrypt data using the certificate store in key vault. Decrypt can be used from a service or services deployed on confidential networks
        /// Do not raise exception
        /// </summary>
        /// <param name="data">data to be encrypted</param>
        /// <param name="token">cancellaton token to stop processing</param>
        /// <returns>Return plain data</returns>
        /// <exception cref="ApplicationException">Returns application exception is certificate is invalid</exception>
        public async Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)> 
            Decrypt(
            byte[] data, 
            CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            X509Certificate2 certificate;
            try
            {
                var result = await _certificateProvider.DownloadCertificate(_options.Value.KeyVaultCertificateIdentifier, token).ConfigureAwait(false);
                if (result.WasDownloaded.HasValue && result.WasDownloaded.Value)
                {
                    certificate = result.Certificate!;
                }
                else 
                {
                    return (false, default, EncryptionDecryptionFail.UnableToGetCertificate);
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default(byte[]?), EncryptionDecryptionFail.UnableToGetCertificate);
            }
            if (certificate == null || !certificate.HasPrivateKey) 
            {
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.GetRSAPrivateKey();
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            try
            {
                var plainData = rsa?.Decrypt(data, RSAEncryptionPadding.OaepSHA512);
                return (true, plainData, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default(byte[]?), EncryptionDecryptionFail.DecryptionFail);
            }
        }

        /// <summary>
        /// Encrypt data using public key of the certificate store in key vault for encryption.
        /// </summary>
        /// <param name="data">data to be encrypted</param>
        /// <param name="token"></param>
        /// <returns>Return encrypted data</returns>
        /// <exception cref="ApplicationException">Returns application exception is certificate is invalid</exception>
        public async Task<(bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason)> 
            Encrypt(byte[] data, 
                    CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            KeyVaultCertificateWithPolicy certificate = null;
            try
            {
                var result = await _certificateProvider.GetCertificate(_options.Value.KeyVaultCertificateIdentifier, token)
                    .ConfigureAwait(false);
                if (result.WasDownloaded.HasValue && result.WasDownloaded.Value) 
                {
                    certificate = result.CertificateWithPolicy!;
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default, EncryptionDecryptionFail.UnableToGetCertificate);
            }
            if (certificate == null || certificate.Cer.Length == 0) 
            {
                return (false, default, EncryptionDecryptionFail.NoPublicKey);
            }
            token.ThrowIfCancellationRequested();
            try
            {
                var x509Cer = new X509Certificate2(certificate.Cer);
                var rsa = x509Cer.GetRSAPublicKey();
                var encryptedData = rsa?.Encrypt(data, RSAEncryptionPadding.OaepSHA512);
                return (true, encryptedData, default);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default, EncryptionDecryptionFail.NoPublicKey);
            }
        }

        public (bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) 
            Encrypt(byte[] publicKey, 
                    byte[] data, 
                    CancellationToken token)
        {
            try
            {
                var certificate = new X509Certificate2(publicKey);
                var rsa = certificate.GetRSAPublicKey();
                var plainData = rsa?.Encrypt(data, RSAEncryptionPadding.OaepSHA512);
                return (true, plainData, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                e.LogException( _logger.LogCritical);
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPublicKey);
            }
        }

        public async Task<(bool wasValid, EncryptionDecryptionFail reason)> 
            VerifySignature(
                byte[] hashValue, 
                byte[] baseSignature, 
                CancellationToken token)
        {
            X509Certificate2 certificate = null;
            try
            {
                var result = await _certificateProvider.GetCertificate(_options.Value.KeyVaultCertificateIdentifier, token).ConfigureAwait(false);
                if(result.WasDownloaded.HasValue && result.WasDownloaded.Value && result.CertificateWithPolicy != null) 
                {
                    certificate = new X509Certificate2(result.CertificateWithPolicy.Cer);
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, EncryptionDecryptionFail.UnableToGetCertificate);
            }
            if (certificate == null) 
            {
                return (false, EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            try
            {
                var rsa = certificate.GetRSAPublicKey();
                bool? result = rsa?.VerifyData(baseSignature, hashValue, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (result.HasValue ? result.Value : false, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default);
            }
        }

        public (bool wasValid, EncryptionDecryptionFail reason) 
            VerifySignature(
                byte[] publicKey, 
                byte[] signature, 
                byte[] baseSignature,
                CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var certificate = new X509Certificate2(publicKey);
                var rsa = certificate.GetRSAPublicKey();
                var result = rsa?.VerifyData(baseSignature, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (result.HasValue ? result.Value : false, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, EncryptionDecryptionFail.NoPublicKey);
            }
        }
    }
}
