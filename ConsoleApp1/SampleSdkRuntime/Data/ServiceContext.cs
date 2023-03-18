using Sample.Sdk.Core;

namespace SampleSdkRuntime.Data
{
    /// <summary>
    /// To add to service collection on the service host
    /// TODO: Create a service context factory method for test
    /// </summary>
    internal class ServiceContext : IServiceContext
    {
        private readonly ServiceRegistration _serviceRegistration;

        internal ServiceContext(ServiceRegistration serviceRegistration)
        {
            _serviceRegistration = serviceRegistration;
        }

        public IEnumerable<byte[]> GetAesKeys()
        {
            return _serviceRegistration?.AesKeys ?? Enumerable.Empty<byte[]>();
        }
    }
}
