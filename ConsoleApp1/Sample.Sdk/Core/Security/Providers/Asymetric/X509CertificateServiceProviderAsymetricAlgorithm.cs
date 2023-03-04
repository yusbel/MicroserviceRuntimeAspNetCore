using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
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
        private readonly CertificateClient _certificateClient;
        private readonly ILogger<X509CertificateServiceProviderAsymetricAlgorithm> _logger;

        public X509CertificateServiceProviderAsymetricAlgorithm(
            IOptions<AzureKeyVaultOptions> options
            , CertificateClient certificateClient
            , ILogger<X509CertificateServiceProviderAsymetricAlgorithm> logger)
        {
            Guard.ThrowWhenNull(options, certificateClient);
            _options = options;
            _certificateClient = certificateClient;
            _logger = logger;
        }

        public async Task<(bool wasCreated, byte[]? data, EncryptionDecryptionFail reason)> 
            CreateSignature(
            byte[] baseString, 
            CancellationToken token)
        {
            Response<X509Certificate2> certificate;
            try
            {
                certificate = await _certificateClient.DownloadCertificateAsync(_options.Value.KeyVaultCertificateIdentifier
                    , null
                    , token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Unable to create signature");
                return (false, default(byte[]?), EncryptionDecryptionFail.UnableToGetCertificate);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.Value.GetRSAPrivateKey();
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
        public async Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)> Decrypt(
            byte[] data, 
            CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            Response<X509Certificate2> certificate;
            try
            {
                //TODO: Add to memory cache
                certificate = await _certificateClient.DownloadCertificateAsync(
                    _options.Value.KeyVaultCertificateIdentifier,
                    null,
                    token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogCritical("Unable to download certificate {}", e);
                return (false, default(byte[]?), EncryptionDecryptionFail.UnableToGetCertificate);
            }
            if (certificate == null || certificate.Value == null || !certificate.Value.HasPrivateKey) 
            {
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.Value.GetRSAPrivateKey();
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error ocurrend when getting private key {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            try
            {
                var plainData = rsa?.Decrypt(data, RSAEncryptionPadding.Pkcs1);
                return (true, plainData, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error occurred when decrypting {e}");
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
        public async Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)> Encrypt(
            byte[] data, 
            CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            Response<KeyVaultCertificateWithPolicy> certificate;
            try
            {
                certificate = await _certificateClient.GetCertificateAsync(_options.Value.KeyVaultCertificateIdentifier, token).ConfigureAwait(false);
            }
            catch (Exception e) 
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "Unable to download certificate");
                return (false, default, EncryptionDecryptionFail.UnableToGetCertificate);
            }
            if (certificate == null || certificate.Value == null || certificate.Value.Cer.Length == 0) 
            {
                return (false, default, EncryptionDecryptionFail.NoPublicKey);
            }
            if (token.IsCancellationRequested) 
                return (false, default, EncryptionDecryptionFail.TaskCancellationWasRequested);
            X509Certificate2 x509Cer;
            try
            {
                x509Cer = new X509Certificate2(certificate.Value.Cer);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to create certificate");
                return (false, default, EncryptionDecryptionFail.UnableToGetCertificate);
            }
            RSA? rsa;
            try
            {
                rsa = x509Cer.GetRSAPublicKey();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error occurred when encrypting");
                return (false, default, EncryptionDecryptionFail.NoPublicKey);
            }
            try
            {
                var encryptedData = rsa?.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                return (true, encryptedData, default);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to encrypt data with public key");
                return (false, default, EncryptionDecryptionFail.EncryptFail);
            } 
        }

        public (bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason) Encrypt(
            byte[] publicKey, 
            byte[] data, 
            CancellationToken token)
        {
            X509Certificate2 certificate;
            try
            {
                certificate = new X509Certificate2(publicKey);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error occurred {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.UnableToGetCertificate);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.GetRSAPublicKey();
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error ocurred when reading public key {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPublicKey);
            }
            try
            {
                var plainData = rsa?.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                return (true, plainData, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error ocurrend when encryptiing {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.EncryptFail);
            }
        }

        public async Task<(bool wasValid, EncryptionDecryptionFail reason)> VerifySignature(
            byte[] hashValue, 
            byte[] baseSignature, 
            CancellationToken token)
        {
            Response<X509Certificate2> certificate;
            try
            {
                certificate = await _certificateClient.DownloadCertificateAsync(_options.Value.KeyVaultCertificateIdentifier
                                                                    , null
                                                                    , token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error occurred");
                return (false, EncryptionDecryptionFail.UnableToGetCertificate);
            }
            HashAlgorithmName algName = new HashAlgorithmName("SHA256");
            if (certificate == null || certificate.Value == null || !certificate.Value.HasPrivateKey) 
            {
                return (false, EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.Value.GetRSAPrivateKey();
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error occurred");
                return (false, EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            try
            {
                bool? result = rsa?.VerifyHash(hashValue, baseSignature, algName, RSASignaturePadding.Pkcs1);
                return (result.HasValue ? result.Value : false, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error occurred verifiying signature");
                return (false, EncryptionDecryptionFail.VerifySignature);
            }
        }

        public (bool wasValid, EncryptionDecryptionFail reason) VerifySignature(
            byte[] publicKey, 
            byte[] signature, 
            byte[] baseSignature,
            CancellationToken token)
        {
            if (token.IsCancellationRequested) 
            {
                return (false, EncryptionDecryptionFail.TaskCancellationWasRequested);
            }
            X509Certificate2 certificate;
            try
            {
                certificate = new X509Certificate2(publicKey);
            }
            catch (Exception e)
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "Failt to create the certificate using the public key");
                return (false, EncryptionDecryptionFail.UnableToGetCertificate);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.GetRSAPublicKey();
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error occurred when getting the public key {e}");
                return (false, EncryptionDecryptionFail.NoPublicKey);
            }
            try
            {
                var result = rsa?.VerifyData(baseSignature, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (result.HasValue ? result.Value : false, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"An error occurred when verifying signature {e}");
                return (false, EncryptionDecryptionFail.VerifySignature);
            }
        }
    }
}
