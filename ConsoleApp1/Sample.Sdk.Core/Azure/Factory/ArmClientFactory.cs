using Azure.Identity;
using Azure.ResourceManager;
using Sample.Sdk.Interface.Azure.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Azure.Factory
{
    public class ArmClientFactory : IArmClientFactory
    {
        private readonly IClientOAuthTokenProviderFactory _oauthTokenProvider;

        public ArmClientFactory(IClientOAuthTokenProviderFactory oauthTokenProvider)
        {
            _oauthTokenProvider = oauthTokenProvider;
        }

        public ArmClient Create()
        {
            _oauthTokenProvider.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientCredentialFlow);
            return new ArmClient(clientCredentialFlow);
        }
    }
}
