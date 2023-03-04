using Microsoft.Graph;

namespace SampleSdkRuntime.Azure.Factory.Interfaces
{
    public interface IGraphServiceClientFactory
    {
        GraphServiceClient? Create();
    }
}