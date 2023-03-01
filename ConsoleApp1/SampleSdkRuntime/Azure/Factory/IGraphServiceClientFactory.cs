using Microsoft.Graph;

namespace SampleSdkRuntime.Azure.Factory
{
    public interface IGraphServiceClientFactory
    {
        GraphServiceClient? Create();
    }
}