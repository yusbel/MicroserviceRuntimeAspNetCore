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
        private readonly IOptions<List<AzureKeyVaultOptions>> _options;
        private readonly ILogger<X509CertificateServiceProviderAsymetricAlgorithm> _logger;
        private readonly ICertificateProvider _certificateProvider;
        private readonly IOptions<CustomProtocolOptions> _protocolOptions;
        private readonly IPublicKeyProvider _publicKeyProvider;

        public X509CertificateServiceProviderAsymetricAlgorithm(
            IOptions<List<AzureKeyVaultOptions>> options
            , ILogger<X509CertificateServiceProviderAsymetricAlgorithm> logger
            , ICertificateProvider certificateProvider
            , IOptions<CustomProtocolOptions> protocolOptions
            , IPublicKeyProvider publicKeyProvider)
        {
            _options = options;
            _logger = logger;
            _certificateProvider = certificateProvider;
            _protocolOptions = protocolOptions;
            _publicKeyProvider = publicKeyProvider;
        }

        public async Task<(bool wasCreated, byte[]? data, EncryptionDecryptionFail reason)> 
            CreateSignature(
            byte[] baseString, 
            Enums.Enums.AzureKeyVaultOptionsType type,
            CancellationToken token)
        {
            X509Certificate2 certificate = null;
            if (string.IsNullOrEmpty(_protocolOptions.Value.SignDataKeyId)) 
            {
                throw new InvalidOperationException("Default certificate name is requried when creating signature");
            }
            try
            {
                var result = await _certificateProvider.DownloadCertificate(_protocolOptions.Value.SignDataKeyId, type, token).ConfigureAwait(false);
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
                var signature = rsa?.SignData(baseString, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (true, signature, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Unable to get private key {e}");
                return (false, default(byte[]?), EncryptionDecryptionFail.NoPrivateKeyFound);
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
            Enums.Enums.AzureKeyVaultOptionsType keyVaultType,
            string certificateName,
            CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            X509Certificate2 certificate;
            var certName = _options.Value.Where(o=> o.Type == keyVaultType)
                                        .Select(c=> c.CertificateNames.Where(cert=> cert == certificateName).First()).First();
            try
            {
                var result = await _certificateProvider.DownloadCertificate(certName, keyVaultType, token).ConfigureAwait(false);
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
                var blockSize = GetByteLength(data.Length, rsa!.KeySize / 8);
                var counter = 0;
                var decryptedData = new List<byte[]>();
                do 
                {
                    var decrypt = data.ToList().Skip(counter * blockSize).Take(blockSize).ToArray();
                    var decryptResult = rsa?.Decrypt(decrypt, RSAEncryptionPadding.OaepSHA384);
                    decryptedData.Add(decryptResult!);
                    counter++;
                } while (counter * blockSize < data.Length);
                var plainData = new List<byte>();
                decryptedData.ForEach(item=> item.ToList().ForEach(plainData.Add));
                return (true, plainData.ToArray(), EncryptionDecryptionFail.None);
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
                    Enums.Enums.AzureKeyVaultOptionsType keyVaultType,
                    string certificateName,
                    CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            KeyVaultCertificateWithPolicy certificate = null;
            var certName = _options.Value.Where(o => o.Type == keyVaultType)
                                        .Select(cert => cert.CertificateNames.Where(c => c == certificateName).First()).First();  
            try
            {
                var result = await _certificateProvider.GetCertificate(certName, keyVaultType, token)
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
                var encryptedData = new List<byte[]>();
                int blockSize = GetByteLength(data.Length, rsa!.KeySize / 8);
                int counter = 0;
                do
                {
                    var encrypt = data.Skip(counter * blockSize).Take(blockSize).ToArray();
                    var encryptResult = rsa?.Encrypt(encrypt, RSAEncryptionPadding.OaepSHA384);
                    encryptedData.Add(encryptResult!);
                    counter++;
                } while (counter * blockSize < data.Length);
                var toReturn = new List<byte>();
                encryptedData.ForEach(item=> 
                { 
                    item.ToList().ForEach(toReturn.Add);
                });
                return (true, toReturn.ToArray(), default);
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
            VerifySignature(string publicKeyUri,
                            string certificateKey,
                            byte[] hashValue,
                            byte[] baseSignature,
                            CancellationToken token)
        {
            var publicKey = await _publicKeyProvider.GetPublicKey(publicKeyUri, certificateKey, token).ConfigureAwait(false);
            var certificate = new X509Certificate2(publicKey);
            var rsa = certificate.GetRSAPublicKey();
            return Verify(hashValue, baseSignature, rsa!);
        }

        public async Task<(bool wasValid, EncryptionDecryptionFail reason)> 
            VerifySignature(
                byte[] hashValue, 
                byte[] baseSignature, 
                Enums.Enums.AzureKeyVaultOptionsType keyVaultOptionsType,
                string certificateName,
                CancellationToken token)
        {
            var certificate = await GetCertificate(keyVaultOptionsType, certificateName, token);
            if (certificate == null)
            {
                return (false, EncryptionDecryptionFail.NoPrivateKeyFound);
            }
            return Verify(hashValue, baseSignature, certificate.GetRSAPublicKey()!);
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
                return Verify(signature, baseSignature, certificate.GetRSAPublicKey()!);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, EncryptionDecryptionFail.NoPublicKey);
            }
        }

        private async Task<X509Certificate2> GetCertificate(Enums.Enums.AzureKeyVaultOptionsType keyVaultOptionsType, string certificateName, CancellationToken token)
        {
            X509Certificate2 certificate = null;
            var certName = _options.Value.Where(o => o.Type == keyVaultOptionsType)
                                          .Select(cert => cert.CertificateNames.Where(c => c == certificateName).First()).First();
            try
            {
                var result = await _certificateProvider.GetCertificate(certName, keyVaultOptionsType, token).ConfigureAwait(false);
                if (result.WasDownloaded.HasValue && result.WasDownloaded.Value && result.CertificateWithPolicy != null)
                {
                    certificate = new X509Certificate2(result.CertificateWithPolicy.Cer);
                    return certificate;
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            
            }
            return default;
        }

        private (bool wasValid, EncryptionDecryptionFail reason) Verify(byte[] hashValue, byte[] baseSignature, RSA rsa)
        {
            try
            {
                bool? result = rsa?.VerifyData(baseSignature, hashValue, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (result.HasValue ? result.Value : false, EncryptionDecryptionFail.None);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default);
            }
        }

        private int GetByteLength(int dataByteSize, int keySize)
        {
            if (dataByteSize <= keySize) 
                return dataByteSize;
            return GetByteLength(dataByteSize / 2, keySize);
        }
    }
}
