namespace Sample.Sdk.Core
{
    public interface IServiceContext
    {
        IEnumerable<byte[]> GetAesKeys();
        string ServiceInstanceName();
        string GetServiceDataBlobContainerName();
        string GetServiceBlobConnStrKey();
    }
}