using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Asymetric
{
    internal class RsaCryptoServiceProviderAsymetricAlgorithm : ISymetricCryptoProvider
    {
        private readonly IOptions<AzureKeyVaultOptions> _options;
        private readonly CertificateClient _certificateClient;

        internal RsaCryptoServiceProviderAsymetricAlgorithm(IOptions<AzureKeyVaultOptions> options, CertificateClient certificateClient)
        {
            _options = options;
            _certificateClient = certificateClient;
        }
        public async Task<byte[]> Decrypt(byte[] data, CancellationToken cancellationToken)
        {
            var keyVaultCertificate = await _certificateClient.GetCertificateAsync("HttpMessageEncryptorCertificate", cancellationToken);
            var certificate = new X509Certificate2(keyVaultCertificate.Value.Cer);
            using var rsaProvider = new RSACryptoServiceProvider();
            rsaProvider.ImportRSAPrivateKey(certificate.GetRSAPrivateKey()?.ExportRSAPrivateKey(), out int bytesRead);
            return rsaProvider.Decrypt(data, true);
        }

        public async Task<byte[]> Encrypt(byte[] data, CancellationToken cancellationToken)
        {
            //retrieve the key from azure and dont store locally?
            var keyVaultCertificate = await _certificateClient.DownloadCertificateAsync("HttpMessageAsymetricEncryptorCertificate");
            using var rsaProvider = new RSACryptoServiceProvider();
            try
            {
                if (keyVaultCertificate.Value != null)
                {
                    var ms = new MemoryStream();
                    keyVaultCertificate.Value.GetRSAPublicKey()?.EncryptValue(data);
                    rsaProvider.ImportPkcs8PrivateKey(ms.ToArray(), out int bytesRead);
                    return rsaProvider.Encrypt(data, true);
                }
            }
            catch (Exception ex) //corrupted data, 
            {
                throw;
            }
            throw new ApplicationException("Invalid certificate");
        }

        public byte[] Encrypt(byte[] publicKey, byte[] data, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public bool TryDecrypt(byte[] data, byte[] key, byte[] iv, out SymetricResult result)
        {
            throw new NotImplementedException();
        }

        public bool TryEncrypt(byte[] data, out SymetricResult result)
        {
            throw new NotImplementedException();
        }
    }
}
