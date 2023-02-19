using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
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
    /// </summary>
    public class X509CertificateServiceProviderAsymetricAlgorithm : IAsymetricCryptoProvider
    {
        private readonly IOptions<AzureKeyVaultOptions> _options;
        private readonly CertificateClient _certificateClient;

        public X509CertificateServiceProviderAsymetricAlgorithm(
            IOptions<AzureKeyVaultOptions> options
            , CertificateClient certificateClient)
        {
            Guard.ThrowWhenNull(options, certificateClient);
            _options = options;
            _certificateClient = certificateClient;
        }

        public async Task<byte[]> CreateSignature(byte[] baseString)
        {
            var certificate = await _certificateClient.DownloadCertificateAsync(_options.Value.KeyVaultCertificateIdentifier
                , null
                , CancellationToken.None);
            if (!certificate.Value.HasPrivateKey) 
            {
                throw new ApplicationException("Certificate does not contain private key");
            }
            var signature = certificate.Value.GetRSAPrivateKey().SignData(baseString, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return signature;
        }

        /// <summary>
        /// Decrypt data using the certificate store in key vault. Decrypt can be used from a service or services deployed on confidential networks
        /// </summary>
        /// <param name="data">data to be encrypted</param>
        /// <param name="token">cancellaton token to stop processing</param>
        /// <returns>Return plain data</returns>
        /// <exception cref="ApplicationException">Returns application exception is certificate is invalid</exception>
        public async Task<byte[]> Decrypt(byte[] data, CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);   
            try
            {
                var certificate = await _certificateClient.DownloadCertificateAsync(_options.Value.KeyVaultCertificateIdentifier, null, token);
                if (certificate.Value != null && certificate.Value.HasPrivateKey 
                    && certificate.Value.GetRSAPrivateKey() != null)
                {
                    return certificate.Value.GetRSAPrivateKey().Decrypt(data, RSAEncryptionPadding.Pkcs1);
                }
                throw new ApplicationException("Invalid certificate or certificate not found");
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Encrypt data using public key of the certificate store in key vault for encryption.
        /// </summary>
        /// <param name="data">data to be encrypted</param>
        /// <param name="token"></param>
        /// <returns>Return encrypted data</returns>
        /// <exception cref="ApplicationException">Returns application exception is certificate is invalid</exception>
        public async Task<byte[]> Encrypt(byte[] data, CancellationToken token)
        {
            Guard.ThrowWhenNull(data, token);
            try
            {
                var certificate = await _certificateClient.GetCertificateAsync(_options.Value.KeyVaultCertificateIdentifier, token);
                if (certificate.Value != null && certificate.Value.Cer.Length > 0)
                {
                    var x509Cer = new X509Certificate2(certificate.Value.Cer);
                    if(x509Cer.GetRSAPublicKey() != null) 
                    {   
                        return x509Cer.GetRSAPublicKey()?.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                    }
                }
                throw new ApplicationException("Invalid certificate or certificate not found");
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public byte[] Encrypt(byte[] publicKey, byte[] data, CancellationToken token)
        {
            var certificate = new X509Certificate2(publicKey);
            if(certificate.GetRSAPublicKey() != null) 
            {
                return certificate.GetRSAPublicKey().Encrypt(data, RSAEncryptionPadding.Pkcs1);
            }
            throw new ApplicationException("Invalid certificate");
        }

        public async Task<bool> VerifySignature(byte[] hashValue, byte[] baseSignature)
        {
            var certificate = await _certificateClient.DownloadCertificateAsync(_options.Value.KeyVaultCertificateIdentifier
                , null
                , CancellationToken.None);
            HashAlgorithmName algName = new HashAlgorithmName("SHA256");
            if (!certificate.Value.HasPrivateKey)
            {
                throw new ApplicationException("Certificate does not contain private key");
            }
            return certificate.Value.GetRSAPrivateKey().VerifyHash(hashValue, baseSignature, algName, RSASignaturePadding.Pkcs1);
            
        }

        public bool VerifySignature(byte[] publicKey, byte[] signature, byte[] baseSignature)
        {
            var certificate = new X509Certificate2(publicKey);
            var result = certificate.GetRSAPublicKey().VerifyData(baseSignature, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return result;
            
        }
    }
}
