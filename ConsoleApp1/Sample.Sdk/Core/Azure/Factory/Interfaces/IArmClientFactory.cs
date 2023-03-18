using Azure.ResourceManager;

namespace Sample.Sdk.Core.Azure.Factory.Interfaces
{
    public interface IArmClientFactory
    {
        ArmClient Create();
    }
}