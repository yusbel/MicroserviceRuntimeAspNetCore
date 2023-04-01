using Azure.ResourceManager;

namespace Sample.Sdk.Interface.Azure.Factory
{
    public interface IArmClientFactory
    {
        ArmClient Create();
    }
}