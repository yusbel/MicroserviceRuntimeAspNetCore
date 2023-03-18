using Microsoft.Graph;

namespace Sample.Sdk.Core.Azure.Factory.Interfaces
{
    public interface IGraphServiceClientFactory
    {
        GraphServiceClient? Create();
    }
}