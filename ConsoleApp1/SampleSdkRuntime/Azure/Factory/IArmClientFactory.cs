using Azure.ResourceManager;

namespace SampleSdkRuntime.Azure.Factory
{
    public interface IArmClientFactory
    {
        ArmClient Create();
    }
}