using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Protocol.Dtos;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class PointToPointSessionRoot
    {
        private readonly ILogger<PointToPointSessionRoot> _logger;

        public PointToPointSessionRoot(ILogger<PointToPointSessionRoot> logger) 
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
                e.LogException(_logger.LogCritical);
                return (false, default, default);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.Value.GetRSAPrivateKey();
            }
            catch (Exception e) 
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "An error ocurred when decrypting with private key. The certificate does not contin a private key");
                return (false, default, default);
            }
            if(token.IsCancellationRequested) 
                return (false, default, default);
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
        protected (bool wasEncrypted, byte[]? data) EncryptWithPublicKey(byte[] cert, byte[] plainText, CancellationToken token)
        {
            X509Certificate2 certificate;
            try
            {
                certificate = new X509Certificate2(cert);
            }
            catch (Exception e)
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "An error ocurred when encrypting with public key");
                return (false, default);
            }
            RSA? rsa;
            try
            {
                rsa = certificate.GetRSAPublicKey();
            }
            catch (Exception e)
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger); 
                return (false, default);
            }
            if (rsa == null)
            {
                _logger.LogCritical("RSA public key return null");
                return (false, default);
            }
            if(token.IsCancellationRequested) 
                return (false, default);
            try
            {
                var encryptedData = rsa.Encrypt(plainText, RSAEncryptionPadding.Pkcs1);
                return (true, encryptedData);
            }
            catch (Exception e)
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "Error ocurred when encrypting data");
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
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "Getting ccertificate fail");
                return (false, default);
            }
        }
        protected async Task<(bool wasValid, string? sessionId)> CreateSessionAndGetSessionIdEncrypted(
            PointToPointSessionDto session
            , HttpClient httpClient
            , CancellationToken token
            , string externalWellknownEndpoint)
        {
            if (session == null || httpClient == null || string.IsNullOrEmpty(externalWellknownEndpoint) || token.IsCancellationRequested) 
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
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "Fail creating session with external service");
                return (false, default);
            }
        }
    }
}