using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.Dtos;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class PointToPointSession : PointToPointSessionRoot, IPointToPointSession
    {
        private readonly ILogger<PointToPointSession> _logger;
        private SessionState _sessionState;
        public SessionState SessionState 
        {
            get 
            {
                return _sessionState;
            }
            private set 
            {
                _sessionState = value;
            }
        }
        public PointToPointSession(ILogger<PointToPointSession> logger
            , ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<PointToPointSessionRoot>())
        {
            _logger = logger;
        }

        public async Task<(bool wasDecrypted, byte[]? content, EncryptionDecryptionFail reason)> DecryptContent(
            string decryptEndpoint
            , byte[] encryptedData
            , IHttpClientResponseConverter httpClientResponseConverter
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token)
        {
            var wellKnownUrl = new Uri(decryptEndpoint);
            var createdOn = DateTime.Now.Ticks;
            if(string.IsNullOrEmpty(_sessionState.SessionIdentifier) 
                || _sessionState.SessionIdentifierEncrypted == null
                || encryptedData == null) 
            {
                return (false, default, default);
            }
            var baseSignature = $"{Convert.ToBase64String(_sessionState.SessionIdentifierEncrypted)}:{createdOn}:{Convert.ToBase64String(encryptedData)}";
            (bool wasCreated, byte[]? data, EncryptionDecryptionFail reason) signature = 
                await cryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSignature), Enums.Enums.AzureKeyVaultOptionsType.ServiceInstance, token);
            if(!signature.wasCreated || signature.data == null) 
            {
                return (false, default, default);
            }
            var data = new EncryptedData() 
            { 
                Encrypted = Convert.ToBase64String(encryptedData),
                Signature = Convert.ToBase64String(signature.data), 
                CreatedOn = createdOn, 
                SessionEncryptedIdentifier = Convert.ToBase64String(_sessionState.SessionIdentifierEncrypted)
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data));
            var result = await httpClientResponseConverter.InvokePost<EncryptedData, InValidHttpResponseMessage>(wellKnownUrl, content, token);
            
            if (!result.isValid || result.invalidResponse == null)
            {
                return (false, default, EncryptionDecryptionFail.SessionIsInvalid);
            }
            if (result.data == null) 
            {
                return (false, default, EncryptionDecryptionFail.DeserializationFail);
            }
            if (token.IsCancellationRequested) 
                return (false, default, EncryptionDecryptionFail.TaskCancellationWasRequested);
            var baseSign = $"{result.data.SessionEncryptedIdentifier}:{result.data.CreatedOn}:{result.data.Encrypted}";
            (bool wasValid, EncryptionDecryptionFail reason) isValidResponse = cryptoProvider.VerifySignature(
                Convert.FromBase64String(_sessionState.ExternalCertWithPublicKeyOnly)
                , Convert.FromBase64String(result.data.Signature)
                , Encoding.UTF8.GetBytes(baseSign), token);

            if (!isValidResponse.wasValid) 
            {
                return (false, default, EncryptionDecryptionFail.VerifySignatureFail);
            }
            (bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason) plainData = 
                await cryptoProvider.Decrypt(Convert.FromBase64String(result.data.Encrypted), 
                Enums.Enums.AzureKeyVaultOptionsType.Runtime, 
                "",
                CancellationToken.None);
            if (!plainData.wasDecrypted || plainData.data == null) 
            {
                return (false, default, default);
            }
            return (true, plainData.data, default);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="externalWellKnownEndpoint"></param>
        /// <param name="certificateClient"></param>
        /// <param name="options"></param>
        /// <param name="httpClient"></param>
        /// <param name="externalServiceKeyProvider"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApplicationException">Certificate miss public or private key</exception>
        public async Task<(bool wasCreated, PointToPointSession? channel, EncryptionDecryptionFail reason)> Create(
            string identifier
            , string externalWellKnownEndpoint
            , CertificateClient certificateClient
            , AzureKeyVaultOptions options
            , HttpClient httpClient // will be disposed on the client code
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , ILoggerFactory loggerFactory
            , CancellationToken token)
        {
            if (token.IsCancellationRequested) 
            {
                return (false, default, EncryptionDecryptionFail.TaskCancellationWasRequested);
            }
            var logger = loggerFactory.CreateLogger<PointToPointSession>();
            Response<X509Certificate2> certificate;
            try
            {
                certificate = await certificateClient.DownloadCertificateAsync(options.DefaultCertificateName, null, token);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default, default);
            }
            token.ThrowIfCancellationRequested();
            if(certificate == null || certificate.Value == null)
                throw new ArgumentNullException(nameof(certificate));
            if(certificate.Value.GetRSAPublicKey() == null)
                throw new ArgumentNullException($"Public key {nameof(certificate)}");
            if (certificate.Value.GetRSAPrivateKey() == null)
                throw new ArgumentNullException($"Private key {nameof(certificate)}");

            // Retrieve public key from wellknown endpoint by passing a query string?action=publickey
            (bool wasReceived, byte[]? data, EncryptionDecryptionFail reason) externalPublicCert = 
                await externalServiceKeyProvider.GetExternalPublicKey($"{externalWellKnownEndpoint}"
                                                                        , httpClient
                                                                        , options
                                                                        , token);
            if (!externalPublicCert.wasReceived || externalPublicCert.data == null) 
            {
                return (false, default, default);
            }
            token.ThrowIfCancellationRequested();
            var sessionIdentifier = Guid.NewGuid().ToString();
            // Encrypt session with the receiver publickey
            var encryptedSession = EncryptWithPublicKey(externalPublicCert.data, Encoding.UTF8.GetBytes(sessionIdentifier), token);
            if (!encryptedSession.wasEncrypted || encryptedSession.data == null) 
            {
                return (false, default, default);
            }
            // Invoke wellknown endpoint with ?action=createSession and publicKey with encryptedSessionIdentifier
            var myCertWithPublicKey = await GetMyCertPublicKey(certificateClient, options, token);
            if (!myCertWithPublicKey.wasValid || myCertWithPublicKey.data == null) 
            {
                return (false, default, default);
            }
            token.ThrowIfCancellationRequested();
            var session = new PointToPointSessionDto()
            {
                EncryptedSessionIdentifier = Convert.ToBase64String(encryptedSession.data),
                PublicKey = Convert.ToBase64String(myCertWithPublicKey.data),
            };
            (bool wasValid, string? sessionId) sessionIdEncryptedWithMyPublicKey = 
                await CreateSessionAndGetSessionIdEncrypted(session
                                                            , httpClient
                                                            , token
                                                            , externalWellKnownEndpoint);
            if (!sessionIdEncryptedWithMyPublicKey.wasValid || string.IsNullOrEmpty(sessionIdEncryptedWithMyPublicKey.sessionId)) 
            {
                return (false, default, default);
            }
            token.ThrowIfCancellationRequested();
            // Wellknown endpoint decrypt the session id using the service private key and encrypt sessionIdentifier using the service publickey
            (bool wasDecrypted, string? data, byte[]? privateKey) sessionIdDecryptedWithMyPrivateKey = 
                await DecryptWithMyPrivateKey(sessionIdEncryptedWithMyPublicKey.sessionId
                                                , certificateClient
                                                , options
                                                , token);
            if (sessionIdDecryptedWithMyPrivateKey.data == null)
                throw new ArgumentNullException($"Session id decrypted return null {nameof(sessionIdDecryptedWithMyPrivateKey)}");
            if (!sessionIdDecryptedWithMyPrivateKey.wasDecrypted)
                return (false, default, default);
            if (sessionIdDecryptedWithMyPrivateKey.privateKey == null)
                throw new ArgumentNullException($"Private key is null {nameof(sessionIdDecryptedWithMyPrivateKey)}");

            SessionState = new SessionState()
                                        {
                                            ExternalCertWithPublicKeyOnly = Convert.ToBase64String(externalPublicCert.data),
                                            MyCertWithPrivateKey = Convert.ToBase64String(sessionIdDecryptedWithMyPrivateKey.privateKey),
                                            MyCertWithPublicKeyOnly = Convert.ToBase64String(myCertWithPublicKey.data),
                                            SessionIdentifier = sessionIdentifier,
                                            SessionIdentifierEncrypted = encryptedSession.data,
                                            Identifier = identifier
                                        };
            return (true, this, default);
        }

    }
}