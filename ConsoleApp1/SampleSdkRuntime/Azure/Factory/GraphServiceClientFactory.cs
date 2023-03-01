using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.Factory
{
    /// <summary>
    /// Factory class for microsoft graph to encapsulate the creation of the object with different Token Credential implementation
    /// </summary>
    public class GraphServiceClientFactory : IGraphServiceClientFactory
    {
        private readonly IClientTokenCredentialFactory _clientTokenDredentialFactory;

        public GraphServiceClientFactory(IClientTokenCredentialFactory clientTokenDredentialFactory)
        {
            _clientTokenDredentialFactory = clientTokenDredentialFactory;
        }
        public GraphServiceClient? Create()
        {
            if(_clientTokenDredentialFactory
                .TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential)) 
            {
                return new GraphServiceClient(clientSecretCredential);
            }
            return default;
        }
    }
}
