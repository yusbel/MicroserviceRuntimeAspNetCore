using Sample.Sdk.Data.Options;

namespace Sample.Sdk.Interface
{
    public interface IServiceContext
    {
        IEnumerable<byte[]> GetAesKeys();
        string GetServiceInstanceName();
        string GetServiceDataBlobContainerName();
        string GetServiceBlobConnStrKey();
        string GetServiceRuntimeBlobConnStrKey();
        ServiceRuntimeConfigData GetServiceRuntimeConfigData();
    }
}