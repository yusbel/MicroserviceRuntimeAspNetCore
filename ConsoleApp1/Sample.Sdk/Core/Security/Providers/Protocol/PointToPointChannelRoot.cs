using Azure.Security.KeyVault.Certificates;
using Sample.Sdk.Core.Azure;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class PointToPointChannelRoot
    {
        protected async Task<Tuple<string, byte[]>> DecryptWithMyPrivateKey(
            string encryptedData
            , CertificateClient client
            , AzureKeyVaultOptions options
            , CancellationToken token)
        {
            var myPrivateKey = await client.DownloadCertificateAsync(options.KeyVaultCertificateIdentifier, null, token);
            try
            {
                var plainData = myPrivateKey.Value.GetRSAPrivateKey().Decrypt(Convert.FromBase64String(encryptedData), RSAEncryptionPadding.Pkcs1);
                return new Tuple<string, byte[]>(Encoding.UTF8.GetString(plainData), myPrivateKey.Value.RawData);
            }
            catch (Exception e)
            {
                throw;
            }
        }
        protected byte[] EncryptWithPublicKey(byte[] cert, byte[] plainText)
        {
            var certificate = new X509Certificate2(cert);
            try
            {
                return certificate.GetRSAPublicKey().Encrypt(plainText, RSAEncryptionPadding.Pkcs1);
            }
            catch (Exception e)
            {
                throw;
            }
        }
        protected async Task<byte[]> GetMyCertPublicKey(CertificateClient certClient
            , AzureKeyVaultOptions options
            , CancellationToken token)
        {
            var cerPublicKey = await certClient.GetCertificateAsync(options.KeyVaultCertificateIdentifier, token);
            return cerPublicKey.Value.Cer;
        }
        protected async Task<string> CreateSessionAndGetSessionIdEncrypted(
            PointToPointSession session
            , HttpClient httpClient
            , CancellationToken token
            , string externalWellknownEndpoint)
        {
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(session));
            var response = await httpClient.PostAsync($"{externalWellknownEndpoint}", content, token);
            var sessionIdentifierEncrypted = await response.Content.ReadAsStringAsync();
            return sessionIdentifierEncrypted;
        }
    }
}