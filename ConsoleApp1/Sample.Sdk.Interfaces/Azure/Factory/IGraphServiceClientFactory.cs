using Microsoft.Graph;

namespace Sample.Sdk.Interface.Azure.Factory
{
    public interface IGraphServiceClientFactory
    {
        GraphServiceClient? Create();
    }
}