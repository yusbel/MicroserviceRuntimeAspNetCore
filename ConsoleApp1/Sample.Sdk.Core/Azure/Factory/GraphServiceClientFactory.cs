using Microsoft.Graph;
using Sample.Sdk.Interface.Azure.Factory;

namespace Sample.Sdk.Core.Azure.Factory
{
    /// <summary>
    /// Factory class for microsoft graph to encapsulate the creation of the object with different Token Credential implementation
    /// </summary>
    public class GraphServiceClientFactory : IGraphServiceClientFactory
    {
        private readonly IClientOAuthTokenProviderFactory _clientTokenDredentialFactory;

        public GraphServiceClientFactory(IClientOAuthTokenProviderFactory clientTokenDredentialFactory)
        {
            _clientTokenDredentialFactory = clientTokenDredentialFactory;
        }
        public GraphServiceClient? Create()
        {
            if (_clientTokenDredentialFactory
                .TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential))
            {
                var graph = new GraphServiceClient(clientSecretCredential);

                return graph;
            }
            return default;
        }
    }
}
