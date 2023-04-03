using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Options;

namespace Sample.Sdk.Data.Registration
{
    public class ServiceRegistration : RuntimeServiceInfo
    {
        public List<byte[]> AesKeys { get; set; } = new List<byte[]>();
        public List<ServiceCredential> Credentials { get; set; } = new List<ServiceCredential>();
        public List<ServiceCryptoSecret> Secrets { get; set; } = new List<ServiceCryptoSecret>();
        public List<ServiceCryptoKey> Keys { get; set; } = new List<ServiceCryptoKey>();
        public List<ServiceCryptoCertificate> Certificates { get; set; } = new List<ServiceCryptoCertificate>();
        public bool WasSuccessful { get; set; }
        public string ServiceInstanceId { get; set; } = string.Empty;
        public string ServiceDataContainerName { get; init; } = string.Empty;
        public string ServiceBlobConnStrConfigKey { get; set; } = string.Empty;
        public ServiceRuntimeConfigData RuntimeConfigData { get; set; } = default!;
        public static ServiceRegistration DefaultInstance(string serviceInstanceId = "")
        {
            serviceInstanceId = string.IsNullOrEmpty(serviceInstanceId) ? Environment.GetEnvironmentVariable(ConfigVar.SERVICE_INSTANCE_NAME_ID)!
                                                                        : serviceInstanceId;
            return new ServiceRegistration()
            {
                ServiceInstanceId = serviceInstanceId,
                ServiceDataContainerName = Environment.GetEnvironmentVariable(ConfigVar.SERVICE_DATA_BLOB_CONTAINER_NAME)!,
                ServiceBlobConnStrConfigKey = Environment.GetEnvironmentVariable(ConfigVar.SERVICE_BLOB_CONN_STR_APP_CONFIG_KEY)!,
                RuntimeConfigData = new ServiceRuntimeConfigData()
                {
                    ServiceRuntimeBlobConnStrKey = Environment.GetEnvironmentVariable(ConfigVar.SERVICE_RUNTIME_BLOB_CONN_STR_KEY)!,
                    ServiceRuntimeBlobPublicKeyContainerName = Environment.GetEnvironmentVariable(ConfigVar.RUNTIME_BLOB_PUBLICKEY_CONTAINER_NAME)!
                }
            };
        }
    }
}
