using Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Interface;
using Sample.Sdk.Interface.Azure.BlobLibs;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Core.Azure.BlobLibs
{
    public class BlobProvider : IBlobProvider
    {
        private readonly CertificateClient _certificateClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _config;
        private readonly IServiceContext _serviceContext;

        public BlobProvider(IAzureClientFactory<CertificateClient> certificateFactory,
            IAzureClientFactory<BlobServiceClient> blobClientFactory,
            IConfiguration config,
            IServiceContext serviceContext)
        {
            _certificateClient = certificateFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
            _blobServiceClient = blobClientFactory.CreateClient(HostTypeOptions.Runtime.ToString());
            _config = config;
            _serviceContext = serviceContext;
        }

        /// <summary>
        /// Retrieve certificate name used for message signature using app configuration client. Read the public\
        /// key of the certificate. Upload the public key to azure blob storage.
        /// </summary>
        /// <param name="certificateNameConfigKey">certificate key</param>
        /// <param name="token">Cancel operation</param>
        /// <exception cref="RequestFailedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public async Task<bool> UploadSignaturePublicKey(string certificateNameConfigKey, CancellationToken token)
        {
            try
            {
                var certificateName = GetCertificateName(certificateNameConfigKey);
                var blobServiceContainer = _blobServiceClient.GetBlobContainerClient(GetBlobContainerName());
                var blobClient = blobServiceContainer.GetBlobClient(GetBlobName(certificateName));
                if (await blobClient.ExistsAsync(token).ConfigureAwait(false))
                {
                    return true;
                }
                var certificate = await _certificateClient.GetCertificateAsync(certificateName, token)
                                                            .ConfigureAwait(false);
                if (certificate != null && certificate.Value != null && certificate.Value.Cer.Length > 0)
                {
                    var certificateToUpload = new BinaryData(certificate.Value.Cer);
                    await blobClient.UploadAsync(certificateToUpload, token).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return true;
        }

        /// <summary>
        /// Download public key from blob storage given the certificate name config key on app configuration.
        /// CertificateNameConfigKey have the value of the certificate name in key vault and is used as blob name.
        /// </summary>
        /// <param name="certificateNameConfigKey">Contain the value of the certificate name</param>
        /// <param name="token">Cancel operation</param>
        /// <returns></returns>
        public async Task<byte[]> DownloadSignaturePublicKey(string certificateNameConfigKey, CancellationToken token)
        {
            var certificateName = GetCertificateName(certificateNameConfigKey);
            var blobName = GetBlobName(certificateName);
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(GetBlobContainerName());
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            if (await blobClient.ExistsAsync(token).ConfigureAwait(false))
            {
                var blob = await blobClient.DownloadContentAsync().ConfigureAwait(false);
                return blob.Value.Content.ToArray();
            }
            return new byte[0];
        }

        private string GetBlobContainerName()
        {
            var containerName = _config.GetValue<string>(_serviceContext.GetServiceRuntimeConfigData().ServiceRuntimeBlobPublicKeyContainerName);
            return containerName;
        }
        private string GetCertificateName(string certificateNameConfigKey)
        {
            return $"{_serviceContext.GetServiceInstanceName()}{_config[certificateNameConfigKey]}";
        }
        private string GetBlobName(string certificateName)
        {
            var blobPath = _config[Environment.GetEnvironmentVariable(ConfigVarConst.BLOB_CERTIFICATE_PATH_APP_CONFIG_KEY)];
            return $@"{blobPath}{_serviceContext.GetServiceInstanceName()}/{certificateName}";
        }
    }
}
