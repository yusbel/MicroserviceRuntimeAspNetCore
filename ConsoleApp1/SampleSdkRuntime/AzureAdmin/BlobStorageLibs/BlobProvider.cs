using Azure;
using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Certificates;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static Sample.Sdk.Core.Enums.Enums;

namespace SampleSdkRuntime.AzureAdmin.BlobStorageLibs
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
            _blobServiceClient = blobClientFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
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
        public async Task<bool> UploadPublicKey(string certificateNameConfigKey, CancellationToken token)
        {
            try
            {
                var blobName = $"{_serviceContext.ServiceInstanceName()}{_config[certificateNameConfigKey]}";
                var blobServiceContainer = _blobServiceClient.GetBlobContainerClient(_serviceContext.GetServiceDataBlobContainerName());
                var blobClient = blobServiceContainer.GetBlobClient($@"Certificates\Signatures\Message\{blobName}");
                if (await blobClient.ExistsAsync(token).ConfigureAwait(false)) 
                {
                    return true;
                }
                var certificate = await _certificateClient.GetCertificateAsync(blobName, token)
                                                            .ConfigureAwait(false);
                if (certificate != null && certificate.Value != null && certificate.Value.Cer.Length > 0)
                {
                    var certificateToUpload = new BinaryData(certificate.Value.Cer);
                    await blobCLient.UploadAsync(certificateToUpload, token);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return true;
        }

        private string? GetEnvironment() 
        {
            return Environment.GetEnvironmentVariable(ConfigurationVariableConstant.ENVIRONMENT_VAR);
        }
    }
}
