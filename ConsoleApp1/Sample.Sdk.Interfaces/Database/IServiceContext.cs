using Sample.Sdk.Data;

namespace Sample.Sdk.Interface.Database
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