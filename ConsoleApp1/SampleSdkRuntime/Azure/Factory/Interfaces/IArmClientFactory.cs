using Azure.ResourceManager;

namespace SampleSdkRuntime.Azure.Factory.Interfaces
{
    public interface IArmClientFactory
    {
        ArmClient Create();
    }
}