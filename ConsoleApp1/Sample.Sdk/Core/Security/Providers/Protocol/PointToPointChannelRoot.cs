using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class PointToPointChannelRoot
    {
        private readonly ILogger<PointToPointChannelRoot> _logger;

        public PointToPointChannelRoot(ILogger<PointToPointChannelRoot> logger) 
        {
            _logger = logger;
        }
        protected async Task<(bool wasDecrypted, string? data, byte[]? privateKey)> DecryptWithMyPrivateKey(
            string encryptedData
            , CertificateClient client
            , AzureKeyVaultOptions options
            , CancellationToken token)
        {
            Response<X509Certificate2> certificate;
            try
            {
                certificate = await client.DownloadCertificateAsync(options.KeyVaultCertificateIdentifier, null, token);
            }
            catch (Exception e) 
            {
                _logger.LogCritical(e, "An error ocurred when downloading the certificate");
                return (false, default, default);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.Value.GetRSAPrivateKey();
            }
            catch (Exception e) 
            {
                _logger.LogCritical(e, "An error ocurred when decrypting with private key. The certificate does not contin a private key");
                return (false, default, default);
            }
            try
            {
                var plainData = rsa?.Decrypt(Convert.FromBase64String(encryptedData), RSAEncryptionPadding.Pkcs1);
                if(plainData == null) 
                {
                    _logger.LogCritical("Decription return no data");
                    return (false, default, default);
                }
                return (true, Encoding.UTF8.GetString(plainData), certificate.Value.RawData);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when decrypting with private key");
                return (false, default, default);
            }
        }
        /// <summary>
        /// Encrypt data using asymetric public key
        /// </summary>
        /// <param name="cert">Public key</param>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns></returns>
        protected (bool wasEncrypted, byte[]? data) EncryptWithPublicKey(byte[] cert, byte[] plainText)
        {   
            try
            {
                var certificate = new X509Certificate2(cert);
                var encryptedData = certificate.GetRSAPublicKey().Encrypt(plainText, RSAEncryptionPadding.Pkcs1);
                return (true, encryptedData);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when encrypting with public key");
                return (false, default);
            }
        }
        protected async Task<(bool wasValid, byte[]? data)> GetMyCertPublicKey(CertificateClient certClient
            , AzureKeyVaultOptions options
            , CancellationToken token)
        {
            if (certClient == null) 
            {
                return (false, default);
            }
            try
            {
                var cerPublicKey = await certClient.GetCertificateAsync(options.KeyVaultCertificateIdentifier, token);
                return (true, cerPublicKey.Value.Cer);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred getting certificate");
                return (false, default);
            }
        }
        protected async Task<(bool wasValid, string? sessionId)> CreateSessionAndGetSessionIdEncrypted(
            PointToPointSession session
            , HttpClient httpClient
            , CancellationToken token
            , string externalWellknownEndpoint)
        {
            if (session == null || httpClient == null || string.IsNullOrEmpty(externalWellknownEndpoint)) 
            {
                return (false, default);
            }
            try
            {
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(session));
                var response = await httpClient.PostAsync($"{externalWellknownEndpoint}", content, token);
                var sessionIdentifierEncrypted = await response.Content.ReadAsStringAsync();
                return (true, sessionIdentifierEncrypted);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when getting the session from wellknown endpoint");
                return (false, default);
            }
        }
    }
}