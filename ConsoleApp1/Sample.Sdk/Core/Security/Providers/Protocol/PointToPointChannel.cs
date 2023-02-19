using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
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
        public TimeSpan Expiry { get; init; }
        public string MyCertWithPrivateKey { get; init; }
        public string MyCertWithPublicKeyOnly { get; init; }
        public string ExternalCertWithPublicKeyOnly { get; init; }
        public byte[] SessionIdentifierEncrypted { get; init; }
        public string SessionIdentifier { get; init; }
        public string Identifier { get; set; }

        public async Task<byte[]> DecryptContent(
            string decryptEndpoint
            , byte[] encryptedData
            , HttpClient httpClient
            , IAsymetricCryptoProvider cryptoProvider)
        {
            var wellKnownUrl = new Uri(decryptEndpoint);
            var createdOn = DateTime.Now.Ticks;
            var baseSignature = $"{Convert.ToBase64String(SessionIdentifierEncrypted)}:{createdOn}:{Convert.ToBase64String(encryptedData)}";
            var signature = await cryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSignature));
            var data = new EncryptedData() 
            { 
                Encrypted = Convert.ToBase64String(encryptedData),
                Signature = Convert.ToBase64String(signature), 
                CreatedOn = createdOn, 
                SessionEncryptedIdentifier = Convert.ToBase64String(SessionIdentifierEncrypted)
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data));
            var response = await httpClient.PostAsync(wellKnownUrl, content);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new ApplicationException($"Content encryption with decrypt endpoint fail with status code {response.StatusCode}");
            }
            var responseEncrypted = System.Text.Json.JsonSerializer.Deserialize<EncryptedData>(await response.Content.ReadAsStringAsync()); 
            if(responseEncrypted == null ) 
            {
                throw new ApplicationException("Invalid response from Decrypt endpoint");
            }
            var baseSign = $"{responseEncrypted.SessionEncryptedIdentifier}:{responseEncrypted.CreatedOn}:{responseEncrypted.Encrypted}";
            var isValidResponse = cryptoProvider.VerifySignature(
                Convert.FromBase64String(ExternalCertWithPublicKeyOnly)
                , Convert.FromBase64String(responseEncrypted.Signature)
                , Encoding.UTF8.GetBytes(baseSign));
            if (!isValidResponse) 
            {
                throw new ApplicationException("Invalid respond from decrypt endpoint");
            }
            var plainData = await cryptoProvider.Decrypt(
                Convert.FromBase64String(responseEncrypted.Encrypted)
                , CancellationToken.None);
            return plainData;
        }

        public async Task<PointToPointChannel> Create(
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
                throw;
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
            var externalPublicCert = await externalServiceKeyProvider.GetExternalPublicKey($"{externalWellKnownEndpoint}", httpClient, options, token);
            // Generate session identifier using Guid.NewGuid
            var sessionIdentifier = Guid.NewGuid().ToString();
            // Encrypt session with the receiver publickey
            var encryptedSession = EncryptWithPublicKey(externalPublicCert, Encoding.UTF8.GetBytes(sessionIdentifier));
            // Invoke wellknown endpoint with ?action=createSession and publicKey with encryptedSessionIdentifier
            var myCertWithPublicKey = await GetMyCertPublicKey(certificateClient, options, token);
            var session = new PointToPointSession()
            {
                EncryptedSessionIdentifier = Convert.ToBase64String(encryptedSession),
                PublicKey = Convert.ToBase64String(myCertWithPublicKey),
            };
            var sessionIdEncryptedWithMyPublicKey = await CreateSessionAndGetSessionIdEncrypted(session
                , httpClient
                , token
                , externalWellKnownEndpoint);
            // Wellknown endpoint decrypt the session id using the service private key and encrypt sessionIdentifier using the service publickey
            var sessionIdDecryptedWithMyPrivateKey = await DecryptWithMyPrivateKey(sessionIdEncryptedWithMyPublicKey
                , certificateClient
                , options
                , token);
            // Valid that the response encrypted session identifier is valid and create a channel with the welKnownendpoitn as identifier, sessionid and both public keys
            if (sessionIdDecryptedWithMyPrivateKey.Item1 == sessionIdentifier)
            {
                return new PointToPointChannel()
                {
                    ExternalCertWithPublicKeyOnly = Convert.ToBase64String(externalPublicCert),
                    MyCertWithPrivateKey = Convert.ToBase64String(sessionIdDecryptedWithMyPrivateKey.Item2),
                    MyCertWithPublicKeyOnly = Convert.ToBase64String(myCertWithPublicKey),
                    SessionIdentifier = sessionIdentifier,
                    SessionIdentifierEncrypted = encryptedSession,
                    Identifier = identifier
                };
            }
            return null;
        }

    }
}