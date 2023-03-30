using Sample.Sdk.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Data
{
    public class ServiceRegistration : IRuntimeServiceInfo
    {
        public List<byte[]> AesKeys { get; set; } = new List<byte[]>();
        public List<ServiceCredential> Credentials { get; set; } = new List<ServiceCredential>();
        public List<ServiceCryptoSecret> Secrets { get;set; } = new List<ServiceCryptoSecret>();
        public List<ServiceCryptoKey> Keys { get; set; } = new List<ServiceCryptoKey>();
        public List<ServiceCryptoCertificate> Certificates { get; set; } = new List<ServiceCryptoCertificate>();
        public bool WasSuccessful { get; set; }
        public string ServiceInstanceId { get; set; } = string.Empty;
        public string ServiceDataContainerName { get; init; } = string.Empty;
        public string ServiceBlobConnStrConfigKey { get; set; } = string.Empty;
        public static ServiceRegistration DefaultInstance(string serviceInstanceId) 
        {
            return new ServiceRegistration()
            {
                ServiceInstanceId = serviceInstanceId,
                ServiceDataContainerName = Environment.GetEnvironmentVariable(ConfigurationVariableConstant.SERVICE_DATA_BLOB_CONTAINER_NAME)!,
                ServiceBlobConnStrConfigKey = Environment.GetEnvironmentVariable(ConfigurationVariableConstant.SERVICE_BLOB_CONN_STR_APP_CONFIG_KEY)!
            };
        }
    }
}
