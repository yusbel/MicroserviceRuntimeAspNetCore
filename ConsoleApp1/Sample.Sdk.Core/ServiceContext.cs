using Sample.Sdk.Data.Options;
using Sample.Sdk.Data.Registration;
using Sample.Sdk.Interface;

namespace SampleSdkRuntime.Data
{
    /// <summary>
    /// To add to service collection on the service host
    /// TODO: Create a service context factory method for test
    /// </summary>
    public class ServiceContext : IServiceContext
    {
        private readonly ServiceRegistration _serviceRegistration;

        public ServiceContext(ServiceRegistration serviceRegistration)
        {
            _serviceRegistration = serviceRegistration;
        }

        public IEnumerable<byte[]> GetAesKeys()
        {
            return _serviceRegistration?.AesKeys ?? Enumerable.Empty<byte[]>();
        }

        public string GetServiceInstanceName() 
        {
            if (_serviceRegistration.ServiceInstanceId.IndexOf("-") > 0) 
            {
                return _serviceRegistration.ServiceInstanceId.Substring(0, _serviceRegistration.ServiceInstanceId.IndexOf("-"));
            }
            return _serviceRegistration.ServiceInstanceId;
        }

        public string GetServiceDataBlobContainerName() 
        {
            return _serviceRegistration.ServiceDataContainerName;
        }

        public string GetServiceBlobConnStrKey() 
        {
            return _serviceRegistration.ServiceBlobConnStrConfigKey;
        }

        public string GetServiceRuntimeBlobConnStrKey()
        {
            return _serviceRegistration.RuntimeConfigData.ServiceRuntimeBlobConnStrKey;
        }

        public ServiceRuntimeConfigData GetServiceRuntimeConfigData()
        {
            return _serviceRegistration.RuntimeConfigData;
        }
    }
}
