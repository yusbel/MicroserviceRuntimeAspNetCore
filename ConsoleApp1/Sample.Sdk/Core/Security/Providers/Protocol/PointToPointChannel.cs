using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class PointToPointChannel : PointToPointChannelRoot, IPointToPointChannel
    {
        private readonly ILogger<PointToPointChannel> _logger;
        private ChannelState _channelState;
        public ChannelState ChannelState 
        {
            get 
            {
                return _channelState;
            }
            private set 
            {
                _channelState = value;
            }
        }
        public PointToPointChannel(ILogger<PointToPointChannel> logger
            , LoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<PointToPointChannelRoot>())
        {
            _logger = logger;
        }


        public async Task<(bool wasDecrypted, byte[]? content, EncryptionDecryptionFail reason)> DecryptContent(
            string decryptEndpoint
            , byte[] encryptedData
            , IHttpClientResponseConverter httpClientResponseConverter
            , IAsymetricCryptoProvider cryptoProvider)
        {
            var wellKnownUrl = new Uri(decryptEndpoint);
            var createdOn = DateTime.Now.Ticks;
            if(string.IsNullOrEmpty(_channelState.SessionIdentifier) 
                || _channelState.SessionIdentifierEncrypted == null
                || encryptedData == null) 
            {
                return (false, default, default);
            }
            var baseSignature = $"{Convert.ToBase64String(_channelState.SessionIdentifierEncrypted)}:{createdOn}:{Convert.ToBase64String(encryptedData)}";
            (bool wasCreated, byte[]? data, EncryptionDecryptionFail reason) signature = 
                await cryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSignature));
            if(!signature.wasCreated || signature.data == null) 
            {
                return (false, default, default);
            }
            var data = new EncryptedData() 
            { 
                Encrypted = Convert.ToBase64String(encryptedData),
                Signature = Convert.ToBase64String(signature.data), 
                CreatedOn = createdOn, 
                SessionEncryptedIdentifier = Convert.ToBase64String(_channelState.SessionIdentifierEncrypted)
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data));
            var result = await httpClientResponseConverter.InvokePost<EncryptedData, InValidHttpResponseMessage>(wellKnownUrl, content);
            
            if (!result.isValid || result.invalidResponse == null)
            {
                return (false, default, EncryptionDecryptionFail.SessionIsInvalid);
            }
            if (result.data == null) 
            {
                return (false, default, EncryptionDecryptionFail.DeserializationFail);
            }
            var baseSign = $"{result.data.SessionEncryptedIdentifier}:{result.data.CreatedOn}:{result.data.Encrypted}";
            (bool wasValid, EncryptionDecryptionFail reason) isValidResponse = cryptoProvider.VerifySignature(
                Convert.FromBase64String(_channelState.ExternalCertWithPublicKeyOnly)
                , Convert.FromBase64String(result.data.Signature)
                , Encoding.UTF8.GetBytes(baseSign));
            if (!isValidResponse.wasValid) 
            {
                return (false, default, EncryptionDecryptionFail.VerifySignature);
            }
            (bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason) plainData = 
                await cryptoProvider.Decrypt(Convert.FromBase64String(result.data.Encrypted)
                                                , CancellationToken.None);
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
        /// <exception cref=""
        /// <exception cref="ApplicationException">Certificate miss public or private key</exception>
        public async Task<(bool wasCreated, PointToPointChannel? channel)> Create(
            string identifier
            , string externalWellKnownEndpoint
            , CertificateClient certificateClient
            , AzureKeyVaultOptions options
            , HttpClient httpClient // will be disposed on the client code
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , ILoggerFactory loggerFactory
            , CancellationToken token)
        {
            var logger = loggerFactory.CreateLogger<PointToPointChannel>();
            Response<X509Certificate2> certificate;
            try
            {
                certificate = await certificateClient.DownloadCertificateAsync(options.KeyVaultCertificateIdentifier, null, token);
            }
            catch (Exception e)
            {
                logger.LogError("Downloading certificate fail with message Message:{} StackTrace: {}", e.Message, e.StackTrace);
                return (false, default);
            }
            logger.LogInformation("====Sucessful download of payroll certificate=====");
            if (certificate == null
                || certificate.Value == null
                || certificate.Value.GetRSAPublicKey() == null
                || certificate.Value.GetRSAPrivateKey() == null)
            {
                throw new ApplicationException("Invalid certificate");
            }
            // Retrieve public key from wellknown endpoint by passing a query string?action=publickey
            (bool wasReceived, byte[]? data, EncryptionDecryptionFail reason) externalPublicCert = 
                await externalServiceKeyProvider.GetExternalPublicKey($"{externalWellKnownEndpoint}"
                                                                        , httpClient
                                                                        , options
                                                                        , token);
            if (!externalPublicCert.wasReceived || externalPublicCert.data == null) 
            {
                return (false, default);
            }
            // Generate session identifier using Guid.NewGuid
            var sessionIdentifier = Guid.NewGuid().ToString();
            // Encrypt session with the receiver publickey
            var encryptedSession = EncryptWithPublicKey(externalPublicCert.data, Encoding.UTF8.GetBytes(sessionIdentifier));
            if (!encryptedSession.wasEncrypted || encryptedSession.data == null) 
            {
                return (false, default);
            }
            // Invoke wellknown endpoint with ?action=createSession and publicKey with encryptedSessionIdentifier
            var myCertWithPublicKey = await GetMyCertPublicKey(certificateClient, options, token);
            if (!myCertWithPublicKey.wasValid || myCertWithPublicKey.data == null) 
            {
                return (false, default);
            }
            var session = new PointToPointSession()
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
                return (false, default);
            }
            // Wellknown endpoint decrypt the session id using the service private key and encrypt sessionIdentifier using the service publickey
            (bool wasDecrypted, string? data, byte[]? privateKey) sessionIdDecryptedWithMyPrivateKey = 
                await DecryptWithMyPrivateKey(sessionIdEncryptedWithMyPublicKey.sessionId
                                                , certificateClient
                                                , options
                                                , token);
            if(sessionIdDecryptedWithMyPrivateKey.data == null 
                || !sessionIdDecryptedWithMyPrivateKey.wasDecrypted
                || sessionIdDecryptedWithMyPrivateKey.privateKey == null) 
            {
                return (false, default);
            }
            ChannelState = new ChannelState()
                                        {
                                            ExternalCertWithPublicKeyOnly = Convert.ToBase64String(externalPublicCert.data),
                                            MyCertWithPrivateKey = Convert.ToBase64String(sessionIdDecryptedWithMyPrivateKey.privateKey),
                                            MyCertWithPublicKeyOnly = Convert.ToBase64String(myCertWithPublicKey.data),
                                            SessionIdentifier = sessionIdentifier,
                                            SessionIdentifierEncrypted = encryptedSession.data,
                                            Identifier = identifier
                                        };
            return (true, this);
        }

    }
}